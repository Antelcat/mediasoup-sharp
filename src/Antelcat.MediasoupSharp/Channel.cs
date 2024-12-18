﻿using System.Collections.Concurrent;
using System.Text.Json.Serialization;
using Antelcat.AutoGen.ComponentModel.Diagnostic;
using Antelcat.LibuvSharp;
using Antelcat.MediasoupSharp.FBS.Log;
using Antelcat.MediasoupSharp.FBS.Message;
using Antelcat.MediasoupSharp.FBS.Notification;
using Antelcat.MediasoupSharp.FBS.Request;
using Antelcat.MediasoupSharp.FBS.Response;
using Antelcat.MediasoupSharp.Internals.Extensions;
using Antelcat.MediasoupSharp.Internals.Utils;
using Antelcat.NodeSharp.Events;
using Google.FlatBuffers;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Threading;

namespace Antelcat.MediasoupSharp;

public class RequestMessage
{
    #region Request

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public uint? Id { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Method? Method { get; init; }

    #endregion

    #region Notification

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Event? Event { get; set; }

    #endregion

    #region Common

    public string? HandlerId { get; set; }

    public ArraySegment<byte> Payload { get; init; }

    #endregion
}

public class Sent
{
    public required RequestMessage RequestMessage { get; init; }

    public required Action<Response> Resolve { get; init; }

    public required Action<Exception> Reject { get; init; }

    public required Action Close { get; init; }
}

[AutoExtractInterface(Interfaces = [typeof(IEventEmitter)])]
public class Channel : EnhancedEventEmitter, IChannel
{
    #region Constants

    private const int RecvBufferMaxLen = PayloadMaxLen * 2;

    private const int MessageMaxLen = PayloadMaxLen + sizeof(int);

    private const int PayloadMaxLen = 1024 * 1024 * 4;

    #endregion Constants

    #region Protected Fields

    /// <summary>
    /// Logger
    /// </summary>
    private readonly ILogger logger = new Logger<Channel>();

    /// <summary>
    /// Closed flag.
    /// </summary>
    private bool closed;

    /// <summary>
    /// Close locker.
    /// </summary>
    private readonly AsyncReaderWriterLock closeLock = new(null);

    /// <summary>
    /// Worker id.
    /// </summary>
    private readonly int workerId;

    /// <summary>
    /// Next id for messages sent to the worker process.
    /// </summary>
    private uint nextId;

    /// <summary>
    /// Map of pending sent requests.
    /// </summary>
    private readonly ConcurrentDictionary<uint, Sent> sents = new();

    /// <summary>
    /// Unix Socket instance for sending messages to the worker process.
    /// </summary>
    private readonly UVStream producerSocket;

    /// <summary>
    /// Unix Socket instance for receiving messages to the worker process.
    /// </summary>
    private readonly UVStream consumerSocket;

    // TODO: CircularBuffer
    /// <summary>
    /// Buffer for reading messages from the worker.
    /// </summary>
    private readonly byte[] recvBuffer;

    private int recvBufferCount;

    #endregion Protected Fields

    private readonly DisposableFlatBufferPool bufferPool = new();

    #region Events

    public event
        Action<string, Antelcat.MediasoupSharp.FBS.Notification.Event,
            Antelcat.MediasoupSharp.FBS.Notification.Notification>? OnNotification;

    #endregion Events

    public Channel(UVStream producerSocket, UVStream consumerSocket, int workerId)
    {
        this.workerId = workerId;

        this.producerSocket = producerSocket;
        this.consumerSocket = consumerSocket;

        recvBuffer      = new byte[RecvBufferMaxLen];
        recvBufferCount = 0;

        this.consumerSocket.Data   += ConsumerSocketOnData;
        this.consumerSocket.Closed += ConsumerSocketOnClosed;
        this.consumerSocket.Error  += ConsumerSocketOnError;
        this.producerSocket.Closed += ProducerSocketOnClosed;
        this.producerSocket.Error  += ProducerSocketOnError;
    }

    public async Task CloseAsync()
    {
        logger.LogDebug($"{nameof(CloseAsync)}() | Worker[{{WorkId}}]", workerId);

        await using (await closeLock.WriteLockAsync())
        {
            if (closed)
            {
                return;
            }

            closed = true;

            // Close every pending sent.
            foreach (var value in sents.Values)
            {
                value.Close();
            }


            // Remove event listeners but leave a fake 'error' handler to avoid
            // propagation.
            consumerSocket.Data   -= ConsumerSocketOnData;
            consumerSocket.Closed -= ConsumerSocketOnClosed;
            consumerSocket.Error  -= ConsumerSocketOnError;

            producerSocket.Closed -= ProducerSocketOnClosed;
            producerSocket.Error  -= ProducerSocketOnError;

            // Destroy the socket after a while to allow pending incoming messages.
            try
            {
                producerSocket.Close();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"{nameof(CloseAsync)}() | Worker[{{WorkerId}}] {nameof(producerSocket)}.Close()",
                    workerId);
            }

            try
            {
                consumerSocket.Close();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"{nameof(CloseAsync)}() | Worker[{{WorkerId}}] {nameof(consumerSocket)}.Close()",
                    workerId);
            }
        }
    }


    public async Task NotifyAsync(
        Func<DisposableFlatBufferBuilder, int?> consumeBuffer,
        Antelcat.MediasoupSharp.FBS.Notification.Event @event,
        Antelcat.MediasoupSharp.FBS.Notification.Body? bodyType,
        string? handlerId,
        int size = 1024)
    {
        logger.LogDebug($"{nameof(NotifyAsync)}() | Worker[{{WorkId}}] Event:{{Event}}", workerId, @event);

        await using (await closeLock.ReadLockAsync())
        {
            if (closed)
            {
                throw new InvalidStateException("Channel closed");
            }

            using var bufferBuilder   = bufferPool.Get(size);
            var       bodyOffset      = consumeBuffer(bufferBuilder);
            var       handlerIdOffset = bufferBuilder.CreateString(handlerId ?? "");

            Offset<Notification> notificationOffset;

            if (bodyType != null && bodyOffset != null)
            {
                notificationOffset = Notification.CreateNotification(
                    bufferBuilder,
                    handlerIdOffset,
                    @event,
                    bodyType.Value,
                    bodyOffset.Value
                );
            }
            else
            {
                notificationOffset = Notification.CreateNotification(
                    bufferBuilder,
                    handlerIdOffset,
                    @event
                );
            }

            var messageOffset = Message.CreateMessage(bufferBuilder,
                Antelcat.MediasoupSharp.FBS.Message.Body.Notification,
                notificationOffset.Value);

            // Finalizes the buffer and adds a 4 byte prefix with the size of the buffer.
            bufferBuilder.FinishSizePrefixed(messageOffset.Value);

            // Zero copy.
            var buffer = bufferBuilder.DataBuffer.ToArraySegment(bufferBuilder.DataBuffer.Position,
                bufferBuilder.DataBuffer.Length - bufferBuilder.DataBuffer.Position);

            // Clear the buffer builder so it's reused for the next request.
            bufferBuilder.Clear();

            if (buffer.Count > MessageMaxLen)
            {
                throw new Exception($"notification too big [event:{@event}]");
            }

            var requestMessage = new RequestMessage
            {
                Event     = @event,
                HandlerId = handlerId,
                Payload   = buffer
            };

            Loop.Default.Sync(() =>
            {
                try
                {
                    // This may throw if closed or remote side ended.
                    producerSocket.Write(
                        requestMessage.Payload,
                        ex =>
                        {
                            if (ex != null)
                            {
                                logger.LogError(ex, $"{nameof(producerSocket)}.Write() | Worker[{{WorkerId}}] Error",
                                    workerId);
                            }
                        }
                    );
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, $"{nameof(producerSocket)}.Write() | Worker[{{WorkerId}}] Error", workerId);
                }
            });
        }
    }


    public async Task<Antelcat.MediasoupSharp.FBS.Response.Response?> RequestAsync(
        Func<DisposableFlatBufferBuilder, int?> consumeBuffer,
        Antelcat.MediasoupSharp.FBS.Request.Method method,
        Antelcat.MediasoupSharp.FBS.Request.Body? bodyType = null,
        string? handlerId = null)
    {
        logger.LogDebug($"{nameof(RequestAsync)}() | Worker[{{WorkId}}] Method:{{Method}}", workerId, method);

        await using (await closeLock.ReadLockAsync())
        {
            if (closed)
            {
                throw new InvalidStateException($"Channel closed, cannot send request [method:{method}]");
            }

            if (nextId < uint.MaxValue) nextId++;
            else nextId = 1;

            var       id              = nextId;
            using var bufferBuilder   = bufferPool.Get();
            var       bodyOffset      = consumeBuffer(bufferBuilder);
            var       handlerIdOffset = bufferBuilder.CreateString(handlerId ?? "");

            Offset<Request> requestOffset;

            if (bodyType != null && bodyOffset != null)
            {
                requestOffset = Request.CreateRequest(
                    bufferBuilder,
                    id,
                    method,
                    handlerIdOffset,
                    bodyType.Value,
                    bodyOffset.Value
                );
            }
            else
            {
                requestOffset = Request.CreateRequest(
                    bufferBuilder,
                    id,
                    method,
                    handlerIdOffset);
            }

            var messageOffset = Message.CreateMessage(
                bufferBuilder,
                Antelcat.MediasoupSharp.FBS.Message.Body.Request,
                requestOffset.Value);

            // Finalizes the buffer and adds a 4 byte prefix with the size of the buffer.
            bufferBuilder.FinishSizePrefixed(messageOffset.Value);

            // Zero copy.
            var buffer = bufferBuilder.DataBuffer.ToArraySegment(
                bufferBuilder.DataBuffer.Position,
                bufferBuilder.DataBuffer.Length - bufferBuilder.DataBuffer.Position);

            // Clear the buffer builder so it's reused for the next request.
            bufferBuilder.Clear();

            bufferPool.Return(bufferBuilder);

            if (buffer.Count > MessageMaxLen)
            {
                throw new Exception($"request too big [method:{method}]");
            }

            var requestMessage = new RequestMessage
            {
                Id        = id,
                Method    = method,
                HandlerId = handlerId,
                Payload   = buffer
            };

            var tcs = new TaskCompletionSource<Response?>();

            var sent = new Sent
            {
                RequestMessage = requestMessage,
                Resolve = data =>
                {
                    if (!sents.Remove(id, out _))
                        return;

                    tcs.TrySetResult(data);
                },
                Reject = Reject,
                Close  = () => tcs.TrySetException(new InvalidStateException("Channel closed"))
            };
            if (!sents.TryAdd(id, sent))
            {
                throw new Exception($"Error add sent request [id:{requestMessage.Id}]");
            }

            tcs.WithTimeout(
                TimeSpan.FromSeconds(15 + 0.1 * sents.Count),
                () => sents.TryRemove(id, out _)
            );

            Loop.Default.Sync(() =>
            {
                try
                {
                    // This may throw if closed or remote side ended.
                    producerSocket.Write(
                        requestMessage.Payload,
                        ex =>
                        {
                            if (ex == null) return;
                            logger.LogError(ex, $"{nameof(producerSocket)}.Write() | Worker[{{WorkerId}}] Error",
                                workerId);
                            Reject(ex);
                        }
                    );
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, $"{nameof(producerSocket)}.Write() | Worker[{{WorkerId}}] Error", workerId);
                    Reject(ex);
                }
            });

            return await tcs.Task;

            void Reject(Exception e)
            {
                if (!sents.Remove(id, out _))
                {
                    return;
                }

                tcs.TrySetException(e);
            }
        }
    }

    public void ProcessMessage(Antelcat.MediasoupSharp.FBS.Message.Message message)
    {
        try
        {
            switch (message.DataType)
            {
                case Antelcat.MediasoupSharp.FBS.Message.Body.Response:
                    ThreadPool.QueueUserWorkItem(_ =>
                    {
                        var response = message.DataAsResponse();
                        ProcessResponse(response);
                    });
                    break;
                case Antelcat.MediasoupSharp.FBS.Message.Body.Notification:
                    ThreadPool.QueueUserWorkItem(_ =>
                    {
                        var notification = message.DataAsNotification();
                        ProcessNotification(notification);
                    });
                    break;
                case Antelcat.MediasoupSharp.FBS.Message.Body.Log:
                    ThreadPool.QueueUserWorkItem(_ =>
                    {
                        var log = message.DataAsLog();
                        ProcessLog(log);
                    });
                    break;
                default:
                {
                    logger.LogWarning("ProcessMessage() | Worker[{WorkerId}] unexpected", workerId);
                }
                    break;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "ProcessMessage() | Worker[{WorkerId}] Received invalid message from the worker process", workerId);
        }
    }

    private void ProcessResponse(Response response)
    {
        if (!sents.TryGetValue(response.Id, out var sent))
        {
            logger.LogError(
                "ProcessResponse() | Worker[{WorkerId}] Received response does not match any sent request [id:{Id}]",
                workerId,
                response.Id
            );
            return;
        }

        if (response.Accepted)
        {
            logger.LogDebug(
                "ProcessResponse() | Worker[{WorkerId}] Request succeed [method:{Method}, id:{Id}]",
                workerId,
                sent.RequestMessage.Method,
                response.Id
            );
            sent.Resolve(response);
        }
        else if (!response.Error.IsNullOrWhiteSpace())
        {
            logger.LogWarning(
                "ProcessResponse() | Worker[{WorkerId}] Request failed [method:{Method}, id:{Id}, reason:\"{Reason}\"]",
                workerId,
                sent.RequestMessage.Method,
                response.Reason,
                response.Id
            );

            sent.Reject(
                new Exception(
                    $"Request failed [method:{sent.RequestMessage.Method}, id:{sent.RequestMessage.Id}, reason:\"{response.Reason}\"]"
                )
            );
        }
        else
        {
            logger.LogError(
                "ProcessResponse() | Worker[{WorkerId}] Received response is not accepted nor rejected [method:{Method}, id:{Id}]",
                workerId,
                sent.RequestMessage.Method,
                response.Id
            );

            sent.Reject(
                new Exception(
                    $"Received response is not accepted nor rejected [method:{sent.RequestMessage.Method}, id:{sent.RequestMessage.Id}]"
                )
            );
        }
    }

    private void ProcessNotification(Notification notification)
    {
        OnNotification?.Invoke(notification.HandlerId, notification.Event, notification);
    }

    private void ProcessLog(Log log)
    {
        var logData = log.Data;
        if (logData is null) return;
        switch (logData[0])
        {
            // 'D' (a debug log).
            case 'D':
            {
                logger.LogDebug("Worker[{WorkerId}] {Flag}", workerId, logData[1..]);

                break;
            }

            // 'W' (a warn log).
            case 'W':
            {
                //TODO: release it when time has come
                //Logger.LogWarning("Worker[{WorkerId}] {Flag}", WorkerId, logData[1..]);

                break;
            }

            // 'E' (a error log).
            case 'E':
            {
                logger.LogError("Worker[{WorkerId}] {Flag}", workerId, logData[1..]);

                break;
            }

            // 'X' (a dump log).
            case 'X':
            {
                // eslint-disable-next-line no-console
                logger.LogTrace("Worker[{WorkerId}] {Flag}", workerId, logData[1..]);

                break;
            }
        }
    }

    private void ConsumerSocketOnData(ArraySegment<byte> data)
    {
        // 数据回调通过单一线程进入，所以 _recvBuffer 是 Thread-safe 的。
        if (recvBufferCount + data.Count > RecvBufferMaxLen)
        {
            logger.LogError(
                $"{nameof(ConsumerSocketOnData)}() | Worker[{{WorkerId}}] Receiving buffer is full, discarding all data into it",
                workerId
            );
            recvBufferCount = 0;
            return;
        }

        Array.Copy(data.Array.NotNull(), data.Offset, recvBuffer, recvBufferCount, data.Count);
        recvBufferCount += data.Count;

        try
        {
            var readCount = 0;
            while (readCount < recvBufferCount - sizeof(int) - 1)
            {
                var msgLen = BitConverter.ToInt32(recvBuffer, readCount);
                readCount += sizeof(int);
                if (readCount >= recvBufferCount)
                {
                    // Incomplete data.
                    break;
                }

                var messageBytes = new byte[msgLen];
                Array.Copy(recvBuffer, readCount, messageBytes, 0, msgLen);
                readCount += msgLen;

                var buf     = new ByteBuffer(messageBytes);
                var message = Message.GetRootAsMessage(buf);
                ProcessMessage(message);
            }

            var remainingLength = recvBufferCount - readCount;
            if (remainingLength == 0)
            {
                recvBufferCount = 0;
            }
            else
            {
                var temp = new byte[remainingLength];
                Array.Copy(recvBuffer, readCount, temp, 0, remainingLength);
                Array.Copy(temp, 0, recvBuffer, 0, remainingLength);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                $"{nameof(ConsumerSocketOnData)}() | Worker[{{WorkerId}}] Invalid data received from the worker process",
                workerId);
        }
    }

    private void ConsumerSocketOnClosed()
    {
        logger.LogDebug(
            $"{nameof(ConsumerSocketOnClosed)}() | Worker[{{WorkerId}}] Consumer Channel ended by the worker process",
            workerId);
    }

    private void ConsumerSocketOnError(Exception? exception)
    {
        logger.LogDebug(exception, $"{nameof(ConsumerSocketOnError)}() | Worker[{{WorkerId}}] Consumer Channel error",
            workerId);
    }

    private void ProducerSocketOnClosed()
    {
        logger.LogDebug(
            $"{nameof(ProducerSocketOnClosed)}() | Worker[{{WorkerId}}] Producer Channel ended by the worker process",
            workerId);
    }

    private void ProducerSocketOnError(Exception? exception)
    {
        logger.LogDebug(exception, $"{nameof(ProducerSocketOnError)}() | Worker[{{WorkerId}}] Producer Channel error",
            workerId);
    }
}