﻿using System.Dynamic;
using System.Text;
using System.Text.Json;
using LibuvSharp;
using MediasoupSharp.Exceptions;
using Microsoft.Extensions.Logging;

namespace MediasoupSharp.Channel;

internal class Channel : EnhancedEventEmitter
{
    private static bool littleEndian = BitConverter.IsLittleEndian;
    private const int MESSAGE_MAX_LEN = 4194308;
    private const int PAYLOAD_MAX_LEN = 4194304;

    private bool closed = false;

    /// <summary>
    /// Unix Socket instance for sending messages to the worker process.
    /// </summary>
    private readonly UVStream producerSocket;

    /// <summary>
    /// Unix Socket instance for receiving messages to the worker process.
    /// </summary>
    private readonly UVStream consumerSocket;

    private int nextId;

    private readonly Dictionary<int, Sent> sents = new();

    /// <summary>
    /// Buffer for reading messages from the worker.
    /// </summary>
    private byte[] recvBuffer = Array.Empty<byte>();


    public Channel(UVStream producerSocket, UVStream consumerSocket, int pid)
    {
        this.producerSocket = producerSocket;
        this.consumerSocket = consumerSocket;

        consumerSocketOnData = bytes =>
        {
            recvBuffer = (recvBuffer.Length == 0 ? bytes : recvBuffer.Concat(bytes)).ToArray();
            if (recvBuffer.Length > PAYLOAD_MAX_LEN)
            {
                Logger?.LogError("receiving buffer is full, discarding all data in it");

                recvBuffer = Array.Empty<byte>();
                return;
            }

            var msgStart = 0;
            while (true)
            {
                var readLen = recvBuffer.Length - msgStart;
                if (readLen < 4)
                {
                    // Incomplete data.
                    break;
                }

                var msgLen = BitConverter.ToInt32(recvBuffer, msgStart);
                if (readLen < 4 + msgLen)
                {
                    // Incomplete data.
                    break;
                }

                var payload = recvBuffer[new Range(msgStart + 4, msgStart + 4 + msgLen)];
                msgStart += 4 + msgLen;
                try
                {
                    switch (payload[0])
                    {
                        // 123 = '{' (a Channel JSON message).
                        case 123:
                            ProcessMessage(
                                JsonSerializer.Deserialize<ExpandoObject>(Encoding.UTF8.GetString(payload))!);
                            break;

                        // 68 = 'D' (a debug log).
                        case 68:
                            Logger?.LogDebug("[pid:{Pid}] {S}", pid,
                                Encoding.UTF8.GetString(payload, 1, payload.Length - 1));
                            break;

                        // 87 = 'W' (a warn log).
                        case 87:
                            Logger?.LogWarning("[pid:{Pid}] {S}", pid,
                                Encoding.UTF8.GetString(payload, 1, payload.Length - 1));
                            break;

                        // 69 = 'E' (an error log).
                        case 69:
                            Logger?.LogError("[pid:{Pid} {S}", pid,
                                Encoding.UTF8.GetString(payload, 1, payload.Length - 1));
                            break;

                        // 88 = 'X' (a dump log).
                        case 88:
                            // eslint-disable-next-line no-console
                            Console.WriteLine(Encoding.UTF8.GetString(payload, 1, payload.Length - 1));
                            break;

                        default:
                            // eslint-disable-next-line no-console
                            Console.WriteLine($"worker[pid:{pid}] unexpected data: %s",
                                Encoding.UTF8.GetString(payload, 1, payload.Length - 1));
                            break;
                    }
                }
                catch (Exception e)
                {
                    Logger?.LogError("received invalid message from the worker process: {S}", e);
                }

                if (msgStart != 0)
                {
                    recvBuffer = recvBuffer.AsSpan(msgStart).ToArray();
                }
            }
        };
        consumerSocketOnClosed = () =>
            Logger?.LogDebug("ConsumerSocketOnClosed() |  Consumer Channel ended by the worker process");
        consumerSocketOnError = exception =>
            Logger?.LogDebug(exception, $"ConsumerSocketOnError() |  Consumer Channel error");
        producerSocketOnClosed = () => Logger?.LogDebug(
            $"ProducerSocketOnClosed() |  Producer Channel ended by the worker process");
        producerSocketOnError = exception =>
            Logger?.LogDebug(exception, $"ProducerSocketOnError() |  Producer Channel error");

        this.consumerSocket.Data += consumerSocketOnData;
        this.consumerSocket.Closed += consumerSocketOnClosed;
        this.consumerSocket.Error += consumerSocketOnError;
        this.producerSocket.Closed += producerSocketOnClosed;
        this.producerSocket.Error += producerSocketOnError;
    }

    public void Close()
    {
        if (closed) return;
        Logger?.LogDebug("close()");

        foreach (var sent in sents.Values)
        {
            sent.Close();
        }

        // Remove event listeners but leave a fake 'error' hander to avoid
        // propagation.
        consumerSocket.Data -= consumerSocketOnData;
        consumerSocket.Closed -= consumerSocketOnClosed;
        consumerSocket.Error -= consumerSocketOnError;

        producerSocket.Closed -= producerSocketOnClosed;
        producerSocket.Error -= producerSocketOnError;

        // Destroy the socket after a while to allow pending incoming messages.
        // 在 Node.js 实现中，延迟了 200 ms。 Feast : 所以我们最好也这样

        Task.Delay(200).ContinueWith(_ =>
        {
            try
            {
                producerSocket.Close();
            }
            catch (Exception e)
            {
                Logger?.LogError(e, $"CloseAsync() |  _producerSocket.Close()");
            }

            try
            {
                consumerSocket.Close();
            }
            catch (Exception e)
            {
                Logger?.LogError(e, $"CloseAsync() |  _consumerSocket.Close()");
            }
        });
    }

    private readonly Action<ArraySegment<byte>> consumerSocketOnData;

    private readonly Action consumerSocketOnClosed;

    private readonly Action<Exception?> consumerSocketOnError;

    private readonly Action producerSocketOnClosed;

    private readonly Action<Exception?> producerSocketOnError;

    public Task<object?> Request(string method, string? handlerId, object? data = null)
    {
        if (nextId < int.MaxValue /*4294967295*/)
            ++nextId;
        else
            nextId = 1;
        var id = nextId;

        Logger?.LogDebug("request() [{Method}, {Id}]", method, id);

        if (closed)
        {
            throw new InvalidStateException("Channel closed");
        }

        var request = $"{id}:{method}:{handlerId}:{(data == null ? string.Empty : data.Serialize())}";
        var buffer = Encoding.UTF8.GetBytes(request);
        if (buffer.Length > MESSAGE_MAX_LEN)
        {
            throw new Exception("Channel request too big");
        }

        // This may throw if closed or remote side ended.
        producerSocket.Write(buffer);
        producerSocket.Write(request);
        var ret = new TaskCompletionSource<object?>();
        Sent sent = new()
        {
            Id = id,
            Method = method,
            Resolve = data2 =>
            {
                if (!sents.Remove(id))
                {
                    return;
                }

                ret.SetResult(data2);
            },
            Reject = error =>
            {
                if (!sents.Remove(id))
                {
                    return;
                }

                ret.SetException(error);
            },
            Close = () => { ret.SetException(new InvalidStateException("Channel closed")); }
        };

        // Add sent stuff to the map.
        sents[id] = sent;

        return ret.Task;
    }

    private void ProcessMessage(dynamic msg)
    {
        // If a response, retrieve its associated request.
        try
        {
            int id = msg.id;
            if (!sents.TryGetValue(id, out var sent))
            {
                Logger?.LogError(
                    "received response does not match any sent request {Id}", id);
                return;
            }

            if (msg.accepted)
            {
                Logger?.LogDebug(
                    "request succeeded {Method},{Id}", sent.Method, sent.Id);

                sent.Resolve(msg.data);
            }
            else if (msg.error)
            {
                string reason = msg.reason;
                Logger?.LogWarning(
                    "request failed [{Method}, {Id}]: {%s}",
                    sent.Method, sent.Id, reason);

                switch ((string)msg.error)
                {
                    case nameof(TypeError):
                        sent.Reject(new TypeError(reason));
                        break;

                    default:
                        sent.Reject(new Exception(reason));
                        break;
                }
            }
            else
            {
                Logger?.LogError(
                    "received response is not accepted nor rejected [{Method}, {Id}]",
                    sent.Method, sent.Id);
            }
        }
        catch (Exception e)
        {
            // If a notification emit it to the corresponding entity.
            if (msg.targetId && msg.Event)
            {
                // Due to how Promises work, it may happen that we receive a response
                // from the worker followed by a notification from the worker. If we
                // emit the notification immediately it may reach its target **before**
                // the response, destroying the ordered delivery. So we must wait a bit
                // here.
                // See https://github.com/versatica/mediasoup/issues/510
                Task.Run(() => this.Emit(msg.targetId.ToString(), msg.Event, msg.data));
            }
            // Otherwise unexpected message.
            else
            {
                Logger?.LogError(
                    "received message is not a response nor a notification");
            }
        }
    }
}