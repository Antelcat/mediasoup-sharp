using System.Collections.Concurrent;
using System.Runtime.Serialization;
using MediasoupSharp.Channel;
using MediasoupSharp.Exceptions;
using Microsoft.Extensions.Logging;

namespace MediasoupSharp.PayloadChannel;

public abstract class PayloadChannelBase : IPayloadChannel
{
    #region Constants

    protected const int MessageMaxLen = PayloadMaxLen + sizeof(int);

    protected const int PayloadMaxLen = 1024 * 1024 * 4;

    #endregion Constants

    #region Protected Fields

    /// <summary>
    /// Logger.
    /// </summary>
    protected readonly ILogger<PayloadChannelBase> Logger;

    /// <summary>
    /// Closed flag.
    /// </summary>
    protected bool Closed;

    /// <summary>
    /// Close locker.
    /// </summary>
    protected readonly AsyncReaderWriterLock CloseLock = new();

    /// <summary>
    /// Worker process PID.
    /// </summary>
    protected readonly int WorkerId;

    /// <summary>
    /// Next id for messages sent to the worker process.
    /// </summary>
    protected uint NextId = 0;

    /// <summary>
    /// Map of pending sent requests.
    /// </summary>
    protected readonly ConcurrentDictionary<uint, Sent> Sents = new();

    /// <summary>
    /// Ongoing notification (waiting for its payload).
    /// </summary>
    protected OngoingNotification? OngoingNotification;

    #endregion Protected Fields

    #region Events

    public abstract event Action<string, string, string?, ArraySegment<byte>>? MessageEvent;

    #endregion Events

    public PayloadChannelBase(ILogger<PayloadChannelBase> logger, int processId)
    {
        Logger = logger;
        WorkerId = processId;
    }

    public virtual void Cleanup()
    {
        // Close every pending sent.
        try
        {
            Sents.Values.ForEach(m => m.Close());
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, $"Cleanup() | Worker[{WorkerId}] _sents.Values.ForEach(m => m.Close.Invoke())");
        }
    }

    public async Task CloseAsync()
    {
        Logger.LogDebug($"CloseAsync() | Worker[{WorkerId}]");

        using (await CloseLock.WriteLockAsync())
        {
            if (Closed)
            {
                return;
            }

            Closed = true;

            Cleanup();
        }
    }

    protected abstract void SendNotification(RequestMessage notification);
    protected abstract void SendRequestMessage(RequestMessage requestMessage, Sent sent);

    private RequestMessage CreateRequestMessage(MethodId methodId, string handlerId, string data, byte[] payload)
    {
        var id = InterlockedExtension.Increment(ref NextId);
        var method = methodId.GetDescription<EnumMemberAttribute>(x => x.Value);

        var requestMesssge = new RequestMessage
        {
            Id = id,
            Method = method,
            HandlerId = handlerId,
            Data = data,
            Payload = payload,
        };

        return requestMesssge;
    }

    private RequestMessage CreateNotification(string @event, string handlerId, string? data, byte[] payload)
    {
        var notification = new RequestMessage
        {
            Event = @event,
            HandlerId = handlerId,
            Data = data,
            Payload = payload,
        };

        return notification;
    }

    public async Task NotifyAsync(string @event, string handlerId, string? data, byte[] payload)
    {
        Logger.LogDebug($"NotifyAsync() | Worker[{WorkerId}] Event:{@event}");

        using (await CloseLock.ReadLockAsync())
        {
            if (Closed)
            {
                throw new InvalidStateException("PayloadChannel closed");
            }

            var notification = CreateNotification(@event, handlerId, data, payload);
            SendNotification(notification);
        }
    }

    public async Task<string?> RequestAsync(MethodId methodId, string handlerId, string data, byte[] payload)
    {
        Logger.LogDebug($"RequestAsync() | Worker[{WorkerId}] Method:{
            methodId.GetDescription<EnumMemberAttribute>(x=>x.Value)}");

        await using (await CloseLock.ReadLockAsync())
        {
            if (Closed)
            {
                throw new InvalidStateException("Channel closed");
            }

            var requestMessage = CreateRequestMessage(methodId, handlerId, data, payload);

            var tcs = new TaskCompletionSource<string?>();
            var sent = new Sent
            {
                RequestMessage = requestMessage,
                Resolve = data =>
                {
                    if (!Sents.TryRemove(requestMessage.Id!.Value, out _))
                    {
                        tcs.TrySetException(new Exception($"Received response does not match any sent request [id:{requestMessage.Id}]"));
                        return;
                    }
                    tcs.TrySetResult(data);
                },
                Reject = e =>
                {
                    if (!Sents.TryRemove(requestMessage.Id!.Value, out _))
                    {
                        tcs.TrySetException(new Exception($"Received response does not match any sent request [id:{requestMessage.Id}]"));
                        return;
                    }
                    tcs.TrySetException(e);
                },
                Close = () =>
                {
                    tcs.TrySetException(new InvalidStateException("Channel closed"));
                },
            };
            if (!Sents.TryAdd(requestMessage.Id!.Value, sent))
            {
                throw new Exception($"Error add sent request [id:{requestMessage.Id}]");
            }
            tcs.WithTimeout(TimeSpan.FromSeconds(15 + (0.1 * Sents.Count)),
                () => Sents.TryRemove(requestMessage.Id!.Value, out _));

            SendRequestMessage(requestMessage, sent);

            return await tcs.Task;
        }
    }

    #region Process Methods

    public abstract void Process(string message, byte[] payload);

    #endregion Process Methods
}