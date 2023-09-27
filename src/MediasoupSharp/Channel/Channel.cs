using System.Text;
using System.Text.Json;
using LightweightUv;
using MediasoupSharp.Errors;
using Microsoft.Extensions.Logging;

// ReSharper disable InconsistentNaming

namespace MediasoupSharp.Channel;

internal class Channel : EnhancedEventEmitter
{
    private readonly ILogger? logger;
    
    private static bool littleEndian = BitConverter.IsLittleEndian;
    private const int MESSAGE_MAX_LEN = 4194308;
    private const int PAYLOAD_MAX_LEN = 4194304;

    private bool closed;

    /// <summary>
    /// Unix Socket instance for sending messages to the worker process.
    /// </summary>
    private readonly UvStream producerSocket;

    /// <summary>
    /// Unix Socket instance for receiving messages to the worker process.
    /// </summary>
    private readonly UvStream consumerSocket;

    private uint nextId;

    private readonly Dictionary<uint, Sent> sents = new();

    /// <summary>
    /// Buffer for reading messages from the worker.
    /// </summary>
    private byte[] recvBuffer = Array.Empty<byte>();

    public Channel(UvStream producerSocket, 
        UvStream consumerSocket, 
        int pid, 
        ILoggerFactory? loggerFactory = null) 
        : base(loggerFactory)
    {
        logger = loggerFactory?.CreateLogger(GetType());

        this.producerSocket = producerSocket;
        this.consumerSocket = consumerSocket;

        consumerSocketOnData = bytes =>
        {
            recvBuffer = (recvBuffer.Length == 0 ? bytes : recvBuffer.Concat(bytes)).ToArray();
            if (recvBuffer.Length > PAYLOAD_MAX_LEN)
            {
                logger?.LogError("receiving buffer is full, discarding all data in it");

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
                            ProcessMessage(JsonDocument.Parse(Encoding.UTF8.GetString(payload)).RootElement);
                            break;

                        // 68 = 'D' (a debug log).
                        case 68:
                            logger?.LogDebug("[pid:{Pid}] {S}", pid,
                                Encoding.UTF8.GetString(payload, 1, payload.Length - 1));
                            break;

                        // 87 = 'W' (a warn log).
                        case 87:
                            logger?.LogWarning("[pid:{Pid}] {S}", pid,
                                Encoding.UTF8.GetString(payload, 1, payload.Length - 1));
                            break;

                        // 69 = 'E' (an error log).
                        case 69:
                            logger?.LogError("[pid:{Pid} {S}", pid,
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
                    logger?.LogError("received invalid message from the worker process: {S}", e);
                }

                if (msgStart != 0)
                {
                    recvBuffer = recvBuffer.AsSpan(msgStart).ToArray();
                }
            }
        };
        consumerSocketOnClosed = () =>
            logger?.LogDebug("ConsumerSocketOnClosed() |  Consumer Channel ended by the worker process");
        consumerSocketOnError = exception =>
            logger?.LogDebug(exception, $"ConsumerSocketOnError() |  Consumer Channel error");
        producerSocketOnClosed = () =>
            logger?.LogDebug("ProducerSocketOnClosed() |  Producer Channel ended by the worker process");
        producerSocketOnError = exception =>
            logger?.LogDebug(exception, $"ProducerSocketOnError() |  Producer Channel error");

        this.consumerSocket.Data   += consumerSocketOnData;
        this.consumerSocket.Closed += consumerSocketOnClosed;
        this.consumerSocket.Error  += consumerSocketOnError;
        this.producerSocket.Closed += producerSocketOnClosed;
        this.producerSocket.Error  += producerSocketOnError;
    }

    public void Close()
    {
        if (closed) return;
        logger?.LogDebug("close()");

        closed = true;
        
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
                logger?.LogError(e, $"CloseAsync() |  _producerSocket.Close()");
            }

            try
            {
                consumerSocket.Close();
            }
            catch (Exception e)
            {
                logger?.LogError(e, $"CloseAsync() |  _consumerSocket.Close()");
            }
        });
    }

    private readonly Action<ArraySegment<byte>> consumerSocketOnData;

    private readonly Action consumerSocketOnClosed;

    private readonly Action<Exception?> consumerSocketOnError;

    private readonly Action producerSocketOnClosed;

    private readonly Action<Exception?> producerSocketOnError;

    public Task<object?> Request(string method, string? handlerId = null, object? data = null)
    {
        if (nextId < uint.MaxValue /*4294967295*/)
            ++nextId;
        else
            nextId = 1;
        var id = nextId;

        logger?.LogDebug("request() [{Method}, {Id}]", method, id);

        if (closed)
        {
            throw new InvalidStateError("Channel closed");
        }

        var request = $"{id}:{method}:{handlerId}:{(data == null ? string.Empty : data.Serialize())}";
        var buffer = Encoding.UTF8.GetBytes(request);
        if (buffer.Length > MESSAGE_MAX_LEN)
        {
            throw new Exception("Channel request too big");
        }

        // This may throw if closed or remote side ended.
        producerSocket.Write(buffer);
        producerSocket.Write(Encoding.UTF8.GetBytes(request));
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
            Close = () => { ret.SetException(new InvalidStateError("Channel closed")); }
        };

        // Add sent stuff to the map.
        sents[id] = sent;

        return ret.Task;
    }

    private void ProcessMessage(JsonElement msg)
    {
        // If a response, retrieve its associated request.
        if (msg.TryGetProperty("id", out var idEle))
        {
            var id = idEle.GetUInt32();
            if (!sents.TryGetValue(id, out var sent))
            {
                logger?.LogError(
                    "received response does not match any sent request {Id}", id);
                return;
            }

            if (msg.TryGetProperty("accepted", out _))
            {
                logger?.LogDebug(
                    "request succeeded {Method},{Id}", sent.Method, sent.Id);

                sent.Resolve(msg.GetProperty("data").GetString());
            }
            else if (msg.TryGetProperty("error", out var errEle))
            {
                string reason = msg.GetProperty("reason").GetString()!;
                logger?.LogWarning(
                    "request failed [{Method}, {Id}]: {%s}",
                    sent.Method, sent.Id, reason);

                switch (errEle.GetString())
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
                logger?.LogError(
                    "received response is not accepted nor rejected [{Method}, {Id}]",
                    sent.Method, sent.Id);
            }
        }

        // If a notification emit it to the corresponding entity.
        if (msg.TryGetProperty("targetId",out var targetIdEle) 
            && !msg.TryGetProperty("event",out var eventEle))
        {
            // Due to how Promises work, it may happen that we receive a response
            // from the worker followed by a notification from the worker. If we
            // emit the notification immediately it may reach its target **before**
            // the response, destroying the ordered delivery. So we must wait a bit
            // here.
            // See https://github.com/versatica/mediasoup/issues/510
            Task.Run(() =>
                Emit(targetIdEle.GetString()!,
                    eventEle.GetString()!,
                    msg.GetProperty("data").GetString()!));
        }
        // Otherwise unexpected message.
        else
        {
            logger?.LogError(
                "received message is not a response nor a notification");
        }
    }

}