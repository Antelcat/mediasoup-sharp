using System.Text;
using FBS.DataProducer;
using FBS.Notification;
using FBS.Request;
using MediasoupSharp.Channel;
using MediasoupSharp.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Threading;

namespace MediasoupSharp.DataProducer;

public class DataProducer : EventEmitter.EventEmitter
{
    /// <summary>
    /// Logger.
    /// </summary>
    private readonly ILogger<DataProducer> logger;

    /// <summary>
    /// Close flag.
    /// </summary>
    private bool closed;

    private readonly AsyncReaderWriterLock closeLock = new();

    /// <summary>
    /// Paused flag.
    /// </summary>
    private bool paused;

    /// <summary>
    /// Internal data.
    /// </summary>
    private readonly DataProducerInternal @internal;

    /// <summary>
    /// DataProducer id.
    /// </summary>
    public string DataProducerId => @internal.DataProducerId;

    /// <summary>
    /// DataProducer data.
    /// </summary>
    public DataProducerData Data { get; }

    /// <summary>
    /// Channel instance.
    /// </summary>
    private readonly IChannel channel;

    /// <summary>
    /// App custom data.
    /// </summary>
    public Dictionary<string, object> AppData { get; }

    /// <summary>
    /// Observer instance.
    /// </summary>
    public EventEmitter.EventEmitter Observer { get; } = new();

    /// <summary>
    /// <para>Events:</para>
    /// <para>@emits transportclose</para>
    /// <para>@emits @close</para>
    /// <para>Observer events:</para>
    /// <para>@emits close</para>
    /// </summary>
    public DataProducer(
        ILoggerFactory loggerFactory,
        DataProducerInternal @internal,
        DataProducerData data,
        IChannel channel,
        bool paused,
        Dictionary<string, object>? appData
    )
    {
        logger = loggerFactory.CreateLogger<DataProducer>();

        this.@internal    = @internal;
        Data         = data;
        this.channel = channel;
        this.paused  = paused;
        AppData      = appData ?? new Dictionary<string, object>();

        HandleWorkerNotifications();
    }

    /// <summary>
    /// Close the DataProducer.
    /// </summary>
    public async Task CloseAsync()
    {
        logger.LogDebug("CloseAsync() | DataProducer:{DataProducerId}", DataProducerId);

        await using(await closeLock.WriteLockAsync())
        {
            if(closed)
            {
                return;
            }

            closed = true;

            // Remove notification subscriptions.
            //_channel.OnNotification -= OnNotificationHandle;

            // Build Request
            var bufferBuilder = channel.BufferPool.Get();

            var closeDataProducerRequest = new FBS.Transport.CloseDataProducerRequestT
            {
                DataProducerId = @internal.DataProducerId,
            };

            var closeDataProducerRequestOffset = FBS.Transport.CloseDataProducerRequest.Pack(bufferBuilder, closeDataProducerRequest);

            // Fire and forget
            channel.RequestAsync(bufferBuilder, Method.TRANSPORT_CLOSE_DATAPRODUCER,
                FBS.Request.Body.Transport_CloseDataProducerRequest,
                closeDataProducerRequestOffset.Value,
                @internal.TransportId
            ).ContinueWithOnFaultedHandleLog(logger);

            Emit("close");

            // Emit observer event.
            Observer.Emit("close");
        }
    }

    /// <summary>
    /// Transport was closed.
    /// </summary>
    public async Task TransportClosedAsync()
    {
        logger.LogDebug("TransportClosedAsync() | DataProducer:{DataProducerId}", DataProducerId);

        await using(await closeLock.WriteLockAsync())
        {
            if(closed)
            {
                return;
            }

            closed = true;

            // Remove notification subscriptions.
            //_channel.OnNotification -= OnNotificationHandle;

            Emit("transportclose");

            // Emit observer event.
            Observer.Emit("close");
        }
    }

    /// <summary>
    /// Dump DataProducer.
    /// </summary>
    public async Task<DumpResponseT> DumpAsync()
    {
        logger.LogDebug("DumpAsync() | DataProducer:{DataProducerId}", DataProducerId);

        await using(await closeLock.ReadLockAsync())
        {
            if(closed)
            {
                throw new InvalidStateException("DataProducer closed");
            }

            // Build Request
            var bufferBuilder = channel.BufferPool.Get();

            var response = await channel.RequestAsync(bufferBuilder, Method.DATAPRODUCER_DUMP,
                null,
                null,
                @internal.DataProducerId);

            /* Decode Response. */
            var data = response.Value.BodyAsDataProducer_DumpResponse().UnPack();
            return data;
        }
    }

    /// <summary>
    /// Get DataProducer stats. Return: DataProducerStat[]
    /// </summary>
    public async Task<GetStatsResponseT[]> GetStatsAsync()
    {
        logger.LogDebug("GetStatsAsync() | DataProducer:{DataProducerId}", DataProducerId);

        await using(await closeLock.ReadLockAsync())
        {
            if(closed)
            {
                throw new InvalidStateException("DataProducer closed");
            }

            // Build Request
            var bufferBuilder = channel.BufferPool.Get();

            var response = await channel.RequestAsync(bufferBuilder, Method.DATAPRODUCER_GET_STATS,
                null,
                null,
                @internal.DataProducerId);

            /* Decode Response. */
            var data = response.Value.BodyAsDataProducer_GetStatsResponse().UnPack();
            return [data];
        }
    }

    /// <summary>
    /// Pause the DataProducer.
    /// </summary>
    public async Task PauseAsync()
    {
        logger.LogDebug("PauseAsync() | DataProducer:{DataProducerId}", DataProducerId);

        await using(await closeLock.ReadLockAsync())
        {
            if(closed)
            {
                throw new InvalidStateException("DataProducer closed");
            }

            // Build Request
            var bufferBuilder = channel.BufferPool.Get();

            await channel.RequestAsync(bufferBuilder, Method.DATACONSUMER_PAUSE,
                null,
                null,
                @internal.DataProducerId);

            var wasPaused = paused;

            paused = true;

            // Emit observer event.
            if(!wasPaused)
            {
                Observer.Emit("pause");
            }
        }
    }

    /// <summary>
    /// Resume the DataProducer.
    /// </summary>
    public async Task ResumeAsync()
    {
        logger.LogDebug("ResumeAsync() | DataProducer:{DataProducerId}", DataProducerId);

        await using(await closeLock.ReadLockAsync())
        {
            if(closed)
            {
                throw new InvalidStateException("DataConsumer closed");
            }

            // Build Request
            var bufferBuilder = channel.BufferPool.Get();

            await channel.RequestAsync(bufferBuilder, Method.DATACONSUMER_RESUME,
                null,
                null,
                @internal.DataProducerId);

            var wasPaused = paused;

            paused = false;

            // Emit observer event.
            if(wasPaused)
            {
                Observer.Emit("resume");
            }
        }
    }

    /// <summary>
    /// Send data (just valid for DataProducers created on a DirectTransport).
    /// </summary>
    public async Task SendAsync(string message, uint? ppid, List<ushort>? subchannels, ushort? requiredSubchannel)
    {
        logger.LogDebug("SendAsync() | DataProducer:{DataProducerId}", DataProducerId);

        /*
         * +-------------------------------+----------+
         * | Value                         | SCTP     |
         * |                               | PPID     |
         * +-------------------------------+----------+
         * | WebRTC String                 | 51       |
         * | WebRTC Binary Partial         | 52       |
         * | (Deprecated)                  |          |
         * | WebRTC Binary                 | 53       |
         * | WebRTC String Partial         | 54       |
         * | (Deprecated)                  |          |
         * | WebRTC String Empty           | 56       |
         * | WebRTC Binary Empty           | 57       |
         * +-------------------------------+----------+
         */

        ppid ??= !message.IsNullOrEmpty() ? 51u : 56u;

        // Ensure we honor PPIDs.
        if(ppid == 56)
        {
            message = " ";
        }

        await SendInternalAsync(Encoding.UTF8.GetBytes(message), ppid.Value, subchannels, requiredSubchannel);
    }

    /// <summary>
    /// Send data (just valid for DataProducers created on a DirectTransport).
    /// </summary>
    public async Task SendAsync(byte[] message, uint? ppid, List<ushort>? subchannels, ushort? requiredSubchannel)
    {
        logger.LogDebug("SendAsync() | DataProducer:{DataProducerId}", DataProducerId);

        ppid ??= !message.IsNullOrEmpty() ? 53u : 57u;

        // Ensure we honor PPIDs.
        if(ppid == 57)
        {
            message = new byte[1];
        }

        await SendInternalAsync(message, ppid.Value, subchannels, requiredSubchannel);
    }

    private async Task SendInternalAsync(byte[] data, uint ppid, List<ushort>? subchannels, ushort? requiredSubchannel)
    {
        await using(await closeLock.ReadLockAsync())
        {
            if(closed)
            {
                throw new InvalidStateException("DataProducer closed");
            }

            // Build Request
            var bufferBuilder = channel.BufferPool.Get();

            var sendNotification = new SendNotificationT
            {
                Ppid               = ppid,
                Data               = data.ToList(),
                Subchannels        = subchannels ?? [],
                RequiredSubchannel = requiredSubchannel,
            };

            var sendNotificationOffset = SendNotification.Pack(bufferBuilder, sendNotification);

            // Fire and forget
            channel.NotifyAsync(bufferBuilder, Event.PRODUCER_SEND,
                FBS.Notification.Body.DataProducer_SendNotification,
                sendNotificationOffset.Value,
                @internal.DataProducerId
            ).ContinueWithOnFaultedHandleLog(logger);
        }
    }

    #region Event Handlers

    private static void HandleWorkerNotifications()
    {
        // No need to subscribe to any event.
    }

    #endregion Event Handlers
}