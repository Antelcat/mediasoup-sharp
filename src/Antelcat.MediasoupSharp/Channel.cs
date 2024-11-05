using System.Collections.Concurrent;
using System.Text.Json.Serialization;
using Antelcat.AutoGen.ComponentModel.Diagnostic;
using Antelcat.LibuvSharp;
using Antelcat.MediasoupSharp.Internals.Extensions;
using Antelcat.NodeSharp.Events;
using FBS.Log;
using FBS.Message;
using FBS.Notification;
using FBS.Request;
using FBS.Response;
using Google.FlatBuffers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using Microsoft.VisualStudio.Threading;

namespace Antelcat.MediasoupSharp;

public class RequestMessage
{
    #region Request

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public uint? Id { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Method? Method { get; set; }

    #endregion

    #region Notification

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Event? Event { get; set; }

    #endregion

    #region Common

    public string? HandlerId { get; set; }

    public ArraySegment<byte> Payload { get; set; }

    #endregion
}

public class Sent
{
    public RequestMessage RequestMessage { get; set; }

    public Action<Response> Resolve { get; set; }

    public Action<Exception> Reject { get; set; }

    public Action Close { get; set; }
}

[AutoExtractInterface(Interfaces = [typeof(IEventEmitter)])]
public class Channel : EnhancedEventEmitter, IChannel
{
    #region Constants

    private const int RecvBufferMaxLen = PayloadMaxLen * 2;
    
    protected const int MessageMaxLen = PayloadMaxLen + sizeof(int);

    protected const int PayloadMaxLen = 1024 * 1024 * 4;

    #endregion Constants

    #region Protected Fields

    /// <summary>
    /// Logger
    /// </summary>
    protected readonly ILogger Logger = new Logger<Channel>();

    /// <summary>
    /// Closed flag.
    /// </summary>
    protected bool Closed;

    /// <summary>
    /// Close locker.
    /// </summary>
    protected readonly AsyncReaderWriterLock CloseLock = new();

    /// <summary>
    /// Worker id.
    /// </summary>
    protected readonly int WorkerId;

    /// <summary>
    /// Next id for messages sent to the worker process.
    /// </summary>
    protected uint NextId;

    /// <summary>
    /// Map of pending sent requests.
    /// </summary>
    protected readonly ConcurrentDictionary<uint, Sent> Sents = new();
    
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

    #region ObjectPool

    private readonly ObjectPoolProvider objectPoolProvider = new DefaultObjectPoolProvider();

    public ObjectPool<FlatBufferBuilder> BufferPool { get; }

    #endregion

    #region Events

    public event Action<string, FBS.Notification.Event, FBS.Notification.Notification>? OnNotification;

    #endregion Events

    private class FlatBufferBuilderPooledObjectPolicy(int initialSize) 
        : IPooledObjectPolicy<FlatBufferBuilder>
    {
        public FlatBufferBuilder Create() => new(initialSize);

        public bool Return(FlatBufferBuilder obj) => true;
    }
    
    public Channel(UVStream producerSocket, UVStream consumerSocket, int workerId)
    {
        WorkerId = workerId;
        
        var policy = new FlatBufferBuilderPooledObjectPolicy(1024);
        BufferPool = objectPoolProvider.Create(policy);
        
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
        Logger.LogDebug("CloseAsync() | Worker[{WorkId}]", WorkerId);

        await using(await CloseLock.WriteLockAsync())
        {
            if(Closed)
            {
                return;
            }

            Closed = true;

            Cleanup();
        }
    }

    public virtual void Cleanup()
    {
        // Close every pending sent.
        try
        {
            foreach (var value in Sents.Values)
            {
                value.Close();
            }
        }
        catch(Exception ex)
        {
            Logger.LogError(ex, "Cleanup() | Worker[{WorkId}] _sents.Values.ForEach(m => m.Close.Invoke())", WorkerId);
        }
        
        // Remove event listeners but leave a fake 'error' hander to avoid
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
            Logger.LogError(ex, $"CloseAsync() | Worker[{{WorkerId}}] {nameof(producerSocket)}.Close()", WorkerId);
        }

        try
        {
            consumerSocket.Close();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, $"CloseAsync() | Worker[{{WorkerId}}] {nameof(consumerSocket)}.Close()", WorkerId);
        }
    }

    public async Task NotifyAsync(FlatBufferBuilder bufferBuilder, FBS.Notification.Event @event, FBS.Notification.Body? bodyType, int? bodyOffset, string? handlerId)
    {
        Logger.LogDebug("NotifyAsync() | Worker[{WorkId}] Event:{Event}", WorkerId, @event);

        await using(await CloseLock.ReadLockAsync())
        {
            if(Closed)
            {
                BufferPool.Return(bufferBuilder);
                throw new InvalidStateException("Channel closed");
            }

            var notificationRequestMessage = CreateNotificationRequestMessage(bufferBuilder, @event, bodyType, bodyOffset, handlerId);
            SendNotification(notificationRequestMessage);
        }
    }

    private void SendNotification(RequestMessage requestMessage)
    {
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
                            Logger.LogError(ex, "_producerSocket.Write() | Worker[{WorkerId}] Error", WorkerId);
                        }
                    }
                );
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "_producerSocket.Write() | Worker[{WorkerId}] Error", WorkerId);
            }
        });
    }

    public async Task<FBS.Response.Response?> RequestAsync(FlatBufferBuilder bufferBuilder, FBS.Request.Method method, FBS.Request.Body? bodyType = null, int? bodyOffset = null, string? handlerId = null)
    {
        Logger.LogDebug("RequestAsync() | Worker[{WorkId}] Method:{Method}", WorkerId, method);

        await using(await CloseLock.ReadLockAsync())
        {
            if(Closed)
            {
                BufferPool.Return(bufferBuilder);
                throw new InvalidStateException("Channel closed");
            }

            var requestMessage = CreateRequestRequestMessage(bufferBuilder, method, bodyType, bodyOffset, handlerId);

            var tcs = new TaskCompletionSource<Response?>();
            var sent = new Sent
            {
                RequestMessage = requestMessage,
                Resolve = data =>
                {
                    if(!Sents.TryRemove(requestMessage.Id!.Value, out _))
                    {
                        tcs.TrySetException(
                            new Exception($"Received response does not match any sent request [id:{requestMessage.Id}]")
                        );
                        return;
                    }

                    tcs.TrySetResult(data);
                },
                Reject = e =>
                {
                    if(!Sents.TryRemove(requestMessage.Id!.Value, out _))
                    {
                        tcs.TrySetException(
                            new Exception($"Received response does not match any sent request [id:{requestMessage.Id}]")
                        );
                        return;
                    }

                    tcs.TrySetException(e);
                },
                Close = () => tcs.TrySetException(new InvalidStateException("Channel closed"))
            };
            if(!Sents.TryAdd(requestMessage.Id!.Value, sent))
            {
                throw new Exception($"Error add sent request [id:{requestMessage.Id}]");
            }

            tcs.WithTimeout(
                TimeSpan.FromSeconds(15 + 0.1 * Sents.Count),
                () => Sents.TryRemove(requestMessage.Id!.Value, out _)
            );

            SendRequest(sent);

            return await tcs.Task;
        }
    }

    private void SendRequest(Sent sent)
    {
        Loop.Default.Sync(() =>
        {
            try
            {
                // This may throw if closed or remote side ended.
                producerSocket.Write(
                    sent.RequestMessage.Payload,
                    ex =>
                    {
                        if (ex == null) return;
                        Logger.LogError(ex, "_producerSocket.Write() | Worker[{WorkerId}] Error", WorkerId);
                        sent.Reject(ex);
                    }
                );
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "producerSocket.Write() | Worker[{WorkerId}] Error", WorkerId);
                sent.Reject(ex);
            }
        });
    }

    #region Event handles

    public void ProcessMessage(FBS.Message.Message message)
    {
        try
        {
            switch(message.DataType)
            {
                case FBS.Message.Body.Response:
                    ThreadPool.QueueUserWorkItem(_ =>
                    {
                        var response = message.DataAsResponse();
                        ProcessResponse(response);
                    });
                    break;
                case FBS.Message.Body.Notification:
                    ThreadPool.QueueUserWorkItem(_ =>
                    {
                        var notification = message.DataAsNotification();
                        ProcessNotification(notification);
                    });
                    break;
                case FBS.Message.Body.Log:
                    ThreadPool.QueueUserWorkItem(_ =>
                    {
                        var log = message.DataAsLog();
                        ProcessLog(log);
                    });
                    break;
                default:
                {
                    Logger.LogWarning("ProcessMessage() | Worker[{WorkerId}] unexpected", WorkerId);
                }
                    break;
            }
        }
        catch(Exception ex)
        {
            Logger.LogError(ex, "ProcessMessage() | Worker[{WorkerId}] Received invalid message from the worker process", WorkerId);
        }
    }

    private void ProcessResponse(Response response)
    {
        if(!Sents.TryGetValue(response.Id, out var sent))
        {
            Logger.LogError(
                "ProcessResponse() | Worker[{WorkerId}] Received response does not match any sent request [id:{Id}]",
                WorkerId,
                response.Id
            );
            return;
        }

        if(response.Accepted)
        {
            Logger.LogDebug(
                "ProcessResponse() | Worker[{WorkerId}] Request succeed [method:{Method}, id:{Id}]",
                WorkerId,
                sent.RequestMessage.Method,
                response.Id
            );
            sent.Resolve(response);
        }
        else if(!response.Error.IsNullOrWhiteSpace())
        {
            Logger.LogWarning(
                "ProcessResponse() | Worker[{WorkerId}] Request failed [method:{Method}, id:{Id}, reason:\"{Reason}\"]",
                WorkerId,
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
            Logger.LogError(
                "ProcessResponse() | Worker[{WorkerId}] Received response is not accepted nor rejected [method:{Method}, id:{Id}]",
                WorkerId,
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
        if(logData is null) return;
        switch(logData[0])
        {
            // 'D' (a debug log).
            case 'D':
            {
                Logger.LogDebug("Worker[{WorkerId}] {Flag}", WorkerId, logData[1..]);

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
                Logger.LogError("Worker[{WorkerId}] {Flag}", WorkerId, logData[1..]);

                break;
            }

            // 'X' (a dump log).
            case 'X':
            {
                // eslint-disable-next-line no-console
                Logger.LogTrace("Worker[{WorkerId}] {Flag}", WorkerId, logData[1..]);

                break;
            }
        }
    }

    #endregion Event handles

    private RequestMessage CreateRequestRequestMessage(
        FlatBufferBuilder bufferBuilder,
        Method method,
        FBS.Request.Body? bodyType,
        int? bodyOffset,
        string? handlerId
    )
    {
        var id = NextId.Increment();

        var handlerIdOffset = bufferBuilder.CreateString(handlerId ?? "");

        Offset<Request> requestOffset;

        if(bodyType.HasValue && bodyOffset.HasValue)
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
            requestOffset = Request.CreateRequest(bufferBuilder, id, method, handlerIdOffset);
        }

        var messageOffset = Message.CreateMessage(bufferBuilder, FBS.Message.Body.Request, requestOffset.Value);

        // Finalizes the buffer and adds a 4 byte prefix with the size of the buffer.
        bufferBuilder.FinishSizePrefixed(messageOffset.Value);

        // Zero copy.
        var buffer = bufferBuilder.DataBuffer.ToArraySegment(bufferBuilder.DataBuffer.Position, bufferBuilder.DataBuffer.Length - bufferBuilder.DataBuffer.Position);

        // Clear the buffer builder so it's reused for the next request.
        bufferBuilder.Clear();

        BufferPool.Return(bufferBuilder);

        if(buffer.Count > MessageMaxLen)
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
        return requestMessage;
    }

    private RequestMessage CreateNotificationRequestMessage(
        FlatBufferBuilder bufferBuilder,
        Event @event,
        FBS.Notification.Body? bodyType,
        int? bodyOffset,
        string? handlerId
    )
    {
        var handlerIdOffset = bufferBuilder.CreateString(handlerId ?? "");

        Offset<Notification> notificationOffset;

        if(bodyType.HasValue && bodyOffset.HasValue)
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

        var messageOffset = Message.CreateMessage(bufferBuilder, FBS.Message.Body.Notification, notificationOffset.Value);

        // Finalizes the buffer and adds a 4 byte prefix with the size of the buffer.
        bufferBuilder.FinishSizePrefixed(messageOffset.Value);

        // Zero copy.
        var buffer = bufferBuilder.DataBuffer.ToArraySegment(bufferBuilder.DataBuffer.Position, bufferBuilder.DataBuffer.Length - bufferBuilder.DataBuffer.Position);

        // Clear the buffer builder so it's reused for the next request.
        bufferBuilder.Clear();

        BufferPool.Return(bufferBuilder);

        if(buffer.Count > MessageMaxLen)
        {
            throw new Exception($"notification too big [event:{@event}]");
        }

        var requestMessage = new RequestMessage
        {
            Event     = @event,
            HandlerId = handlerId,
            Payload   = buffer
        };
        return requestMessage;
    }
    
       #region Event handles

    private void ConsumerSocketOnData(ArraySegment<byte> data)
    {
        // 数据回调通过单一线程进入，所以 _recvBuffer 是 Thread-safe 的。
        if (recvBufferCount + data.Count > RecvBufferMaxLen)
        {
            Logger.LogError(
                "ConsumerSocketOnData() | Worker[{WorkerId}] Receiving buffer is full, discarding all data into it",
                WorkerId
            );
            recvBufferCount = 0;
            return;
        }

        Array.Copy(data.Array!, data.Offset, recvBuffer, recvBufferCount, data.Count);
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
            Logger.LogError(ex,
                "ConsumerSocketOnData() | Worker[{WorkerId}] Invalid data received from the worker process", WorkerId);
        }
    }

    private void ConsumerSocketOnClosed()
    {
        Logger.LogDebug("ConsumerSocketOnClosed() | Worker[{WorkerId}] Consumer Channel ended by the worker process",
            WorkerId);
    }

    private void ConsumerSocketOnError(Exception? exception)
    {
        Logger.LogDebug(exception, "ConsumerSocketOnError() | Worker[{WorkerId}] Consumer Channel error", WorkerId);
    }

    private void ProducerSocketOnClosed()
    {
        Logger.LogDebug("ProducerSocketOnClosed() | Worker[{WorkerId}] Producer Channel ended by the worker process",
            WorkerId);
    }

    private void ProducerSocketOnError(Exception? exception)
    {
        Logger.LogDebug(exception, "ProducerSocketOnError() | Worker[{WorkerId}] Producer Channel error", WorkerId);
    }

    #endregion Event handles
}
