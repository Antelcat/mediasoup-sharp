using System.Text;
using Antelcat.AutoGen.ComponentModel.Diagnostic;
using Antelcat.MediasoupSharp.Internals.Extensions;
using FBS.DataConsumer;
using FBS.Notification;
using FBS.Request;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Threading;

namespace Antelcat.MediasoupSharp;

using DataConsumerObserver = EnhancedEventEmitter<DataConsumerObserverEvents>;

public class DataConsumerOptions<TDataConsumerAppData>
{
    /// <summary>
    /// The id of the DataProducer to consume.
    /// </summary>
    public required string DataProducerId { get; set; }

    /// <summary>
    /// Just if consuming over SCTP.
    /// Whether data messages must be received in order. If true the messages will
    /// be sent reliably. Defaults to the value in the DataProducer if it has type
    /// 'sctp' or to true if it has type 'direct'.
    /// </summary>
    public bool? Ordered { get; set; }

    /// <summary>
    /// Just if consuming over SCTP.
    /// When ordered is false indicates the time (in milliseconds) after which a
    /// SCTP packet will stop being retransmitted. Defaults to the value in the
    /// DataProducer if it has type 'sctp' or unset if it has type 'direct'.
    /// </summary>
    public int? MaxPacketLifeTime { get; set; }

    /// <summary>
    /// Just if consuming over SCTP.
    /// When ordered is false indicates the maximum number of times a packet will
    /// be retransmitted. Defaults to the value in the DataProducer if it has type
    /// 'sctp' or unset if it has type 'direct'.
    /// </summary>
    public int? MaxRetransmits { get; set; }

    /// <summary>
    /// Whether the data consumer must start in paused mode. Default false.
    /// </summary>
    /// <value></value>
    public bool Paused { get; set; }

    /**
     * Subchannels this data consumer initially subscribes to.
     * Only used in case this data consumer receives messages from a local data
     * producer that specifies subchannel(s) when calling send().
     */
    public List<ushort>? Subchannels { get; set; }

    /// <summary>
    /// Custom application data.
    /// </summary>
    public TDataConsumerAppData? AppData { get; set; }
}

public abstract class DataConsumerEvents
{
    public          object?              TransportClose;
    public          object?              DataProducerClose;
    public          object?              DataProducerPause;
    public          object?              DataProducerResume;
    public required MessageNotificationT Message;
    public          object?              SctpSendBufferFull;
    public          uint                 BufferedAmountLow;
    public          (string, Exception)  ListenerError;

    // Private events.
    internal object? close;
    internal object? dataProducerClose;
}

public abstract class DataConsumerObserverEvents
{
    public object? Close;
    public object? Pause;
    public object? Resume;
}

public class DataConsumerInternal : TransportInternal
{
    /// <summary>
    /// DataConsumer id.
    /// </summary>
    public required string DataConsumerId { get; set; }
}

public class DataConsumerData
{
    /// <summary>
    /// Associated DataProducer id.
    /// </summary>
    public required string DataProducerId { get; init; }

    public FBS.DataProducer.Type Type { get; set; }

    /// <summary>
    /// SCTP stream parameters.
    /// </summary>
    public SctpStreamParameters? SctpStreamParameters { get; init; }

    /// <summary>
    /// DataChannel label.
    /// </summary>
    public required string Label { get; init; }

    /// <summary>
    /// DataChannel protocol.
    /// </summary>
    public required string Protocol { get; init; }

    public uint BufferedAmountLowThreshold { get; set; }
}


[AutoExtractInterface]
public class DataConsumer<TDataConsumerAppData>
    : EnhancedEventEmitter<DataConsumerEvents>, IDataConsumer
    where TDataConsumerAppData : new()
{
    /// <summary>
    /// Logger.
    /// </summary>
    private readonly ILogger logger = new Logger<DataConsumer<TDataConsumerAppData>>();

    /// <summary>
    /// Internal data.
    /// </summary>
    private readonly DataConsumerInternal @internal;

    /// <summary>
    /// DataConsumer id.
    /// </summary>
    public string Id => @internal.DataConsumerId;

    /// <summary>
    /// DataConsumer data.
    /// </summary>
    public DataConsumerData Data { get; set; }

    /// <summary>
    /// Channel instance.
    /// </summary>
    private readonly IChannel channel;

    /// <summary>
    /// Close flag
    /// </summary>
    private bool closed;

    private readonly AsyncReaderWriterLock closeLock = new();

    /// <summary>
    /// Paused flag.
    /// </summary>
    private bool paused;

    /// <summary>
    /// Associated DataProducer paused flag.
    /// </summary>
    private bool dataProducerPaused;

    /// <summary>
    /// Subchannels subscribed to.
    /// </summary>
    private List<ushort> subchannels;

    /// <summary>
    /// App custom data.
    /// </summary>
    public TDataConsumerAppData AppData { get; }

    /// <summary>
    /// Observer instance.
    /// </summary>
    public DataConsumerObserver Observer { get; } = new();

    /// <summary>
    /// <para>Events:</para>
    /// <para>@emits <see cref="DataConsumerEvents.TransportClose"/></para>
    /// <para>@emits <see cref="DataConsumerEvents.DataProducerClose"/></para>
    /// <para>@emits <see cref="DataConsumerEvents.Message"/> - (message: Buffer, ppid: number)</para>
    /// <para>@emits <see cref="DataConsumerEvents.SctpSendBufferFull"/></para>
    /// <para>@emits <see cref="DataConsumerEvents.BufferedAmountLow"/> - (bufferedAmount: number)</para>
    /// <para>@emits <see cref="DataConsumerEvents.close"/></para>
    /// <para>@emits <see cref="DataConsumerEvents.dataProducerClose"/></para>
    /// <para>Observer events:</para>
    /// <para>@emits <see cref="DataConsumerObserverEvents.Close"/></para>
    /// <para>@emits <see cref="DataConsumerObserverEvents.Pause"/></para>
    /// <para>@emits <see cref="DataConsumerObserverEvents.Resume"/></para>
    /// </summary>
    public DataConsumer(
        DataConsumerInternal @internal,
        DataConsumerData data,
        IChannel channel,
        bool paused,
        bool dataProducerPaused,
        List<ushort> subchannels,
        TDataConsumerAppData? appData
    )
    {
        this.@internal          = @internal;
        Data                    = data;
        this.channel            = channel;
        this.paused             = paused;
        this.dataProducerPaused = dataProducerPaused;
        this.subchannels        = subchannels;
        AppData                 = appData ?? new();

        HandleWorkerNotifications();
    }

    /// <summary>
    /// Close the DataConsumer.
    /// </summary>
    public async Task CloseAsync()
    {
        logger.LogDebug("CloseAsync() | DataConsumer:{DataConsumerId}", Id);

        await using (await closeLock.WriteLockAsync())
        {
            if (closed)
            {
                return;
            }

            closed = true;

            // Remove notification subscriptions.
            channel.OnNotification -= OnNotificationHandle;

            // Build Request
            var bufferBuilder = channel.BufferPool.Get();

            var closeDataConsumerRequest = new FBS.Transport.CloseDataConsumerRequestT
            {
                DataConsumerId = @internal.DataConsumerId
            };

            var closeDataConsumerRequestOffset =
                FBS.Transport.CloseDataConsumerRequest.Pack(bufferBuilder, closeDataConsumerRequest);

            // Fire and forget
            channel.RequestAsync(
                bufferBuilder,
                Method.TRANSPORT_CLOSE_DATACONSUMER,
                FBS.Request.Body.Transport_CloseDataConsumerRequest,
                closeDataConsumerRequestOffset.Value,
                @internal.TransportId
            ).ContinueWithOnFaultedHandleLog(logger);

            this.Emit(static x => x.close);

            // Emit observer event.
            Observer.Emit(static x => x.Close);
        }
    }

    /// <summary>
    /// Transport was closed.
    /// </summary>
    public async Task TransportClosedAsync()
    {
        logger.LogDebug("TransportClosedAsync() | DataConsumer:{DataConsumerId}", Id);

        await using (await closeLock.WriteLockAsync())
        {
            if (closed)
            {
                return;
            }

            closed = true;

            // Remove notification subscriptions.
            channel.OnNotification -= OnNotificationHandle;

            this.Emit(static x => x.TransportClose);

            // Emit observer event.
            Observer.Emit(static x => x.Close);
        }
    }

    /// <summary>
    /// Dump DataConsumer.
    /// </summary>
    public async Task<FBS.DataConsumer.DumpResponseT> DumpAsync()
    {
        logger.LogDebug("DumpAsync() | DataConsumer:{DataConsumerId}", Id);

        await using (await closeLock.ReadLockAsync())
        {
            if (closed)
            {
                throw new InvalidStateException("DataConsumer closed");
            }

            // Build Request
            var bufferBuilder = channel.BufferPool.Get();

            var response = await channel.RequestAsync(bufferBuilder, Method.DATACONSUMER_DUMP,
                null,
                null,
                @internal.DataConsumerId);

            /* Decode Response. */
            var data = response.NotNull().BodyAsDataConsumer_DumpResponse().UnPack();
            return data;
        }
    }

    /// <summary>
    /// Get DataConsumer stats. Return: DataConsumerStat[]
    /// </summary>
    public async Task<FBS.DataConsumer.GetStatsResponseT[]> GetStatsAsync()
    {
        logger.LogDebug("GetStatsAsync() | DataConsumer:{DataConsumerId}", Id);

        await using (await closeLock.ReadLockAsync())
        {
            if (closed)
            {
                throw new InvalidStateException("DataConsumer closed");
            }

            // Build Request
            var bufferBuilder = channel.BufferPool.Get();

            var response = await channel.RequestAsync(bufferBuilder, Method.DATACONSUMER_GET_STATS,
                null,
                null,
                @internal.DataConsumerId);

            // Decode Response
            var data = response.NotNull().BodyAsDataConsumer_GetStatsResponse().UnPack();

            return [data];
        }
    }

    /// <summary>
    /// Pause the DataConsumer.
    /// </summary>
    public async Task PauseAsync()
    {
        logger.LogDebug($"{nameof(PauseAsync)}() | DataConsumer:{{DataConsumerId}}", Id);

        await using (await closeLock.ReadLockAsync())
        {
            if (closed)
            {
                throw new InvalidStateException("DataConsumer closed");
            }

            // Build Request
            var bufferBuilder = channel.BufferPool.Get();

            /* Ignore Response. */
            _ = await channel.RequestAsync(bufferBuilder, Method.DATACONSUMER_PAUSE,
                null,
                null,
                @internal.DataConsumerId);

            var wasPaused = paused;

            paused = true;

            // Emit observer event.
            if (!wasPaused && !dataProducerPaused)
            {
                Observer.Emit(static x => x.Pause);
            }
        }
    }

    /// <summary>
    /// Resume the DataConsumer.
    /// </summary>
    public async Task ResumeAsync()
    {
        logger.LogDebug($"{nameof(ResumeAsync)}() | DataConsumer:{{DataConsumerId}}", Id);

        await using (await closeLock.ReadLockAsync())
        {
            if (closed)
            {
                throw new InvalidStateException("DataConsumer closed");
            }

            // Build Request
            var bufferBuilder = channel.BufferPool.Get();

            await channel.RequestAsync(bufferBuilder, Method.DATACONSUMER_PAUSE,
                null,
                null,
                @internal.DataConsumerId);

            var wasPaused = paused;

            paused = false;

            // Emit observer event.
            if (wasPaused && !dataProducerPaused)
            {
                Observer.Emit(static x => x.Resume);
            }
        }
    }

    /// <summary>
    /// Set buffered amount low threshold.
    /// </summary>
    public async Task SetBufferedAmountLowThresholdAsync(uint threshold)
    {
        logger.LogDebug($"{nameof(SetBufferedAmountLowThresholdAsync)}() | Threshold:{{Threshold}}", threshold);

        await using (await closeLock.ReadLockAsync())
        {
            if (closed)
            {
                throw new InvalidStateException("DataConsumer closed");
            }

            // Build Request
            var bufferBuilder = channel.BufferPool.Get();

            var setBufferedAmountLowThresholdRequest = new SetBufferedAmountLowThresholdRequestT
            {
                Threshold = threshold
            };

            var setBufferedAmountLowThresholdRequestOffset =
                SetBufferedAmountLowThresholdRequest.Pack(bufferBuilder, setBufferedAmountLowThresholdRequest);

            // Fire and forget
            channel.RequestAsync(
                bufferBuilder,
                Method.DATACONSUMER_SET_BUFFERED_AMOUNT_LOW_THRESHOLD,
                FBS.Request.Body.DataConsumer_SetBufferedAmountLowThresholdRequest,
                setBufferedAmountLowThresholdRequestOffset.Value,
                @internal.DataConsumerId
            ).ContinueWithOnFaultedHandleLog(logger);
        }
    }

    /// <summary>
    /// Send data (just valid for DataProducers created on a DirectTransport).
    /// </summary>
    public async Task SendAsync(string message, uint? ppid)
    {
        logger.LogDebug($"{nameof(SendAsync)}() | DataConsumer:{{DataConsumerId}}", Id);

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
        if (ppid == 56)
        {
            message = " ";
        }

        await SendInternalAsync(Encoding.UTF8.GetBytes(message), ppid.Value);
    }

    /// <summary>
    /// Send data (just valid for DataProducers created on a DirectTransport).
    /// </summary>
    public async Task SendAsync(byte[] message, uint? ppid)
    {
        logger.LogDebug($"{nameof(SendAsync)}() | DataConsumer:{{DataConsumerId}}", Id);

        ppid ??= !message.IsNullOrEmpty() ? 53u : 57u;

        // Ensure we honor PPIDs.
        if (ppid == 57)
        {
            message = new byte[1];
        }

        await SendInternalAsync(message, ppid.Value);
    }

    private async Task SendInternalAsync(byte[] data, uint ppid)
    {
        await using (await closeLock.ReadLockAsync())
        {
            if (closed)
            {
                throw new InvalidStateException("DataConsumer closed");
            }

            // Build Request
            var bufferBuilder = channel.BufferPool.Get();

            var sendRequest = new SendRequestT
            {
                Ppid = ppid,
                Data = data.ToList()
            };

            var sendRequestOffset = SendRequest.Pack(bufferBuilder, sendRequest);

            // Fire and forget
            channel.RequestAsync(bufferBuilder, Method.DATACONSUMER_SEND,
                FBS.Request.Body.DataConsumer_SendRequest,
                sendRequestOffset.Value,
                @internal.DataConsumerId
            ).ContinueWithOnFaultedHandleLog(logger);
        }
    }

    /// <summary>
    /// Get buffered amount size.
    /// </summary>
    public async Task<uint> GetBufferedAmountAsync()
    {
        logger.LogDebug($"{nameof(GetBufferedAmountAsync)}()");

        await using (await closeLock.ReadLockAsync())
        {
            if (closed)
            {
                throw new InvalidStateException("DataConsumer closed");
            }

            // Build Request
            var bufferBuilder = channel.BufferPool.Get();

            var response = await channel.RequestAsync(bufferBuilder, Method.DATACONSUMER_GET_BUFFERED_AMOUNT,
                null,
                null,
                @internal.DataConsumerId);

            /* Decode Response. */
            var data = response.NotNull().BodyAsDataConsumer_GetBufferedAmountResponse().UnPack();
            return data.BufferedAmount;
        }
    }

    /// <summary>
    /// Set subchannels.
    /// </summary>
    public async Task SetSubchannelsAsync(List<ushort> subchannels)
    {
        logger.LogDebug("SetSubchannelsAsync()");

        await using (await closeLock.ReadLockAsync())
        {
            if (closed)
            {
                throw new InvalidStateException("DataConsumer closed");
            }

            // Build Request
            var bufferBuilder = channel.BufferPool.Get();

            var setSubchannelsRequest = new SetSubchannelsRequestT
            {
                Subchannels = subchannels
            };

            var setSubchannelsRequestOffset = SetSubchannelsRequest.Pack(bufferBuilder, setSubchannelsRequest);

            var response = await channel.RequestAsync(bufferBuilder, Method.DATACONSUMER_SET_SUBCHANNELS,
                FBS.Request.Body.DataConsumer_SetSubchannelsRequest,
                setSubchannelsRequestOffset.Value,
                @internal.DataConsumerId);

            /* Decode Response. */
            var data = response.NotNull().BodyAsDataConsumer_SetSubchannelsResponse().UnPack();
            // Update subchannels.
            this.subchannels = data.Subchannels;
        }
    }

    /// <summary>
    /// Add a subchannel.
    /// </summary>
    public async Task AddSubchannelAsync(ushort subchannel)
    {
        logger.LogDebug("AddSubchannelAsync()");

        await using (await closeLock.ReadLockAsync())
        {
            if (closed)
            {
                throw new InvalidStateException("DataConsumer closed");
            }

            // Build Request
            var bufferBuilder = channel.BufferPool.Get();

            var addSubchannelsRequest = new AddSubchannelRequestT
            {
                Subchannel = subchannel
            };

            var addSubchannelRequestOffset = AddSubchannelRequest.Pack(bufferBuilder, addSubchannelsRequest);

            var response = await channel.RequestAsync(bufferBuilder, Method.DATACONSUMER_ADD_SUBCHANNEL,
                FBS.Request.Body.DataConsumer_AddSubchannelRequest,
                addSubchannelRequestOffset.Value,
                @internal.DataConsumerId);

            /* Decode Response. */
            var data = response.NotNull().BodyAsDataConsumer_AddSubchannelResponse().UnPack();
            // Update subchannels.
            subchannels = data.Subchannels;
        }
    }

    /// <summary>
    /// Remove a subchannel.
    /// </summary>
    public async Task RemoveSubchannelAsync(ushort subchannel)
    {
        logger.LogDebug($"{nameof(RemoveSubchannelAsync)}()");

        await using (await closeLock.ReadLockAsync())
        {
            if (closed)
            {
                throw new InvalidStateException("DataConsumer closed");
            }

            // Build Request
            var bufferBuilder = channel.BufferPool.Get();

            var removeSubchannelsRequest = new RemoveSubchannelRequestT
            {
                Subchannel = subchannel
            };

            var removeSubchannelRequestOffset = RemoveSubchannelRequest.Pack(bufferBuilder, removeSubchannelsRequest);

            var response = await channel.RequestAsync(bufferBuilder, Method.DATACONSUMER_REMOVE_SUBCHANNEL,
                FBS.Request.Body.DataConsumer_RemoveSubchannelRequest,
                removeSubchannelRequestOffset.Value,
                @internal.DataConsumerId);

            /* Decode Response. */
            var data = response.NotNull().BodyAsDataConsumer_AddSubchannelResponse().UnPack();
            // Update subchannels.
            subchannels = data.Subchannels;
        }
    }

    #region Event Handlers

    private void HandleWorkerNotifications()
    {
        channel.OnNotification += OnNotificationHandle;
    }

#pragma warning disable VSTHRD100 // Avoid async void methods
    private async void OnNotificationHandle(string handlerId, Event @event, Notification notification)
#pragma warning restore VSTHRD100 // Avoid async void methods
    {
        if (handlerId != Id)
        {
            return;
        }

        switch (@event)
        {
            case Event.DATACONSUMER_DATAPRODUCER_CLOSE:
            {
                await using (await closeLock.WriteLockAsync())
                {
                    if (closed)
                    {
                        break;
                    }

                    closed = true;

                    // Remove notification subscriptions.
                    channel.OnNotification -= OnNotificationHandle;

                    this.Emit(static x => x.dataProducerClose);
                    this.Emit(static x => x.DataProducerClose);

                    // Emit observer event.
                    Observer.Emit(static x => x.Close);
                }

                break;
            }
            case Event.DATACONSUMER_DATAPRODUCER_PAUSE:
            {
                if (dataProducerPaused)
                {
                    break;
                }

                dataProducerPaused = true;

                this.Emit(static x => x.DataProducerPause);

                // Emit observer event.
                if (!paused)
                {
                    Observer.Emit(static x => x.Pause);
                }

                break;
            }

            case Event.DATACONSUMER_DATAPRODUCER_RESUME:
            {
                if (!dataProducerPaused)
                {
                    break;
                }

                dataProducerPaused = false;

                this.Emit(static x => x.DataProducerResume);

                // Emit observer event.
                if (!paused)
                {
                    Observer.Emit(static x => x.Resume);
                }

                break;
            }
            case Event.DATACONSUMER_SCTP_SENDBUFFER_FULL:
            {
                this.Emit(static x => x.SctpSendBufferFull);

                break;
            }
            case Event.DATACONSUMER_BUFFERED_AMOUNT_LOW:
            {
                var bufferedAmountLowNotification =
                    notification.BodyAsDataConsumer_BufferedAmountLowNotification().UnPack();

                this.Emit(static x => x.BufferedAmountLow, bufferedAmountLowNotification.BufferedAmount);

                break;
            }
            case Event.DATACONSUMER_MESSAGE:
            {
                var messageNotification = notification.BodyAsDataConsumer_MessageNotification().UnPack();
                this.Emit(static x => x.Message, messageNotification);

                break;
            }
            default:
            {
                logger.LogError(
                    $"{nameof(OnNotificationHandle)}() | Ignoring unknown event \"{{Event}}\" in channel listener",
                    @event);
                break;
            }
        }
    }

    #endregion Event Handlers
}