using System.Text;
using Antelcat.AutoGen.ComponentModel.Diagnostic;
using Antelcat.MediasoupSharp.FBS.DataProducer;
using Antelcat.MediasoupSharp.FBS.Notification;
using Antelcat.MediasoupSharp.FBS.Request;
using Antelcat.MediasoupSharp.FBS.SctpParameters;
using Antelcat.MediasoupSharp.Internals.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Threading;

namespace Antelcat.MediasoupSharp;


public class DataProducerInternal : TransportInternal
{
    /// <summary>
    /// DataProducer id.
    /// </summary>
    public required string DataProducerId { get; init; }
}

public record DataProducerData
{
    public Antelcat.MediasoupSharp.FBS.DataProducer.Type Type { get; set; }

    /// <summary>
    /// SCTP stream parameters.
    /// </summary>
    public SctpStreamParametersT? SctpStreamParameters { get; init; }

    /// <summary>
    /// DataChannel label.
    /// </summary>
    public required string Label { get; init; }

    /// <summary>
    /// DataChannel protocol.
    /// </summary>
    public required string Protocol { get; init; }
}

[AutoExtractInterface(NamingTemplate = nameof(IDataProducer))]
public class DataProducerImpl<TDataProducerAppData> 
    : EnhancedEventEmitter<DataProducerEvents>, 
        IDataProducer<TDataProducerAppData>
    where TDataProducerAppData : new()
{
    /// <summary>
    /// Logger.
    /// </summary>
    private readonly ILogger logger = new Logger<IDataProducer>();

    /// <summary>
    /// Close flag.
    /// </summary>
    private bool closed;

    private readonly AsyncReaderWriterLock closeLock = new(null);

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
    public string Id => @internal.DataProducerId;

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
    public TDataProducerAppData AppData { get; set; }

    /// <summary>
    /// Observer instance.
    /// </summary>
    public DataProducerObserver Observer { get; } = new();

    /// <summary>
    /// <para>Events : <see cref="DataProducerEvents"/></para>
    /// <para>Observer events : <see cref="DataProducerObserverEvents"/></para>
    /// </summary>
    public DataProducerImpl(
        DataProducerInternal @internal,
        DataProducerData data,
        IChannel channel,
        bool paused,
        TDataProducerAppData? appData
    )
    {
        this.@internal = @internal;
        Data           = data;
        this.channel   = channel;
        this.paused    = paused;
        AppData        = appData ?? new();

        HandleWorkerNotifications();
        HandleListenerError();
    }

    /// <summary>
    /// Close the DataProducer.
    /// </summary>
    public async Task CloseAsync()
    {
        logger.LogDebug("CloseAsync() | DataProducer:{DataProducerId}", Id);

        await using (await closeLock.WriteLockAsync())
        {
            if (closed)
            {
                return;
            }

            closed = true;

            // Remove notification subscriptions.
            //_channel.OnNotification -= OnNotificationHandle;

            // Fire and forget
            channel.RequestAsync(bufferBuilder => Antelcat.MediasoupSharp.FBS.Transport.CloseDataProducerRequest
                    .Pack(bufferBuilder, new Antelcat.MediasoupSharp.FBS.Transport.CloseDataProducerRequestT
                    {
                        DataProducerId = @internal.DataProducerId
                    }).Value, 
                Method.TRANSPORT_CLOSE_DATAPRODUCER,
                Antelcat.MediasoupSharp.FBS.Request.Body.Transport_CloseDataProducerRequest,
                @internal.TransportId
            ).ContinueWithOnFaultedHandleLog(logger);

            this.Emit(static x => x.close);

            // Emit observer event.
            Observer.SafeEmit(static x=>x.Close);
        }
    }

    /// <summary>
    /// Transport was closed.
    /// </summary>
    public async Task TransportClosedAsync()
    {
        logger.LogDebug("TransportClosedAsync() | DataProducer:{DataProducerId}", Id);

        await using (await closeLock.WriteLockAsync())
        {
            if (closed)
            {
                return;
            }

            closed = true;

            // Remove notification subscriptions.
            //_channel.OnNotification -= OnNotificationHandle;

            this.SafeEmit(static x=>x.TransportClose);

            // Emit observer event.
            Observer.SafeEmit(static x=>x.Close);
        }
    }

    /// <summary>
    /// Dump DataProducer.
    /// </summary>
    public async Task<Antelcat.MediasoupSharp.FBS.DataProducer.DumpResponseT> DumpAsync()
    {
        logger.LogDebug($"{nameof(DumpAsync)}() | DataProducer:{{DataProducerId}}", Id);

        await using (await closeLock.ReadLockAsync())
        {
            if (closed)
            {
                throw new InvalidStateException("DataProducer closed");
            }

            var response = await channel.RequestAsync(static _ => null, 
                Method.DATAPRODUCER_DUMP,
                null,
                @internal.DataProducerId);

            /* Decode Response. */
            var data = response.NotNull().BodyAsDataProducer_DumpResponse().UnPack();
            return data;
        }
    }

    /// <summary>
    /// Get DataProducer stats. Return: DataProducerStat[]
    /// </summary>
    public async Task<Antelcat.MediasoupSharp.FBS.DataProducer.GetStatsResponseT[]> GetStatsAsync()
    {
        logger.LogDebug($"{nameof(GetStatsAsync)}() | DataProducer:{{DataProducerId}}", Id);

        await using (await closeLock.ReadLockAsync())
        {
            if (closed)
            {
                throw new InvalidStateException("DataProducer closed");
            }

            var response = await channel.RequestAsync(static _ => null, 
                Method.DATAPRODUCER_GET_STATS,
                null,
                @internal.DataProducerId);

            /* Decode Response. */
            var data = response.NotNull().BodyAsDataProducer_GetStatsResponse().UnPack();
            return [data];
        }
    }

    /// <summary>
    /// Pause the DataProducer.
    /// </summary>
    public async Task PauseAsync()
    {
        logger.LogDebug($"{nameof(PauseAsync)}() | DataProducer:{{DataProducerId}}", Id);

        await using (await closeLock.ReadLockAsync())
        {
            if (closed)
            {
                throw new InvalidStateException("DataProducer closed");
            }

            await channel.RequestAsync(static _ => null,
                Method.DATACONSUMER_PAUSE,
                null,
                @internal.DataProducerId);

            var wasPaused = paused;

            paused = true;

            // Emit observer event.
            if (!wasPaused)
            {
                Observer.SafeEmit(static x=>x.Pause);
            }
        }
    }

    /// <summary>
    /// Resume the DataProducer.
    /// </summary>
    public async Task ResumeAsync()
    {
        logger.LogDebug($"{nameof(ResumeAsync)}() | DataProducer:{{DataProducerId}}", Id);

        await using (await closeLock.ReadLockAsync())
        {
            if (closed)
            {
                throw new InvalidStateException("DataConsumer closed");
            }

            await channel.RequestAsync(static _ => null, 
                Method.DATACONSUMER_RESUME,
                null,
                @internal.DataProducerId);

            var wasPaused = paused;

            paused = false;

            // Emit observer event.
            if (wasPaused)
            {
                Observer.SafeEmit(static x=>x.Resume);
            }
        }
    }

    /// <summary>
    /// Send data (just valid for DataProducers created on a DirectTransport).
    /// </summary>
    public async Task SendAsync(string message,
                                uint? ppid = null,
                                List<ushort>? subchannels = null,
                                ushort? requiredSubchannel = null)
    {
        logger.LogDebug($"{nameof(SendAsync)}() | DataProducer:{{DataProducerId}}", Id);

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

        await SendInternalAsync(Encoding.UTF8.GetBytes(message), ppid.Value, subchannels, requiredSubchannel);
    }

    /// <summary>
    /// Send data (just valid for DataProducers created on a DirectTransport).
    /// </summary>
    public async Task SendAsync(byte[] message, uint? ppid, List<ushort>? subchannels, ushort? requiredSubchannel)
    {
        logger.LogDebug("SendAsync() | DataProducer:{DataProducerId}", Id);

        ppid ??= !message.IsNullOrEmpty() ? 53u : 57u;

        // Ensure we honor PPIDs.
        if (ppid == 57)
        {
            message = new byte[1];
        }

        await SendInternalAsync(message, ppid.Value, subchannels, requiredSubchannel);
    }

    private async Task SendInternalAsync(byte[] data, uint ppid, List<ushort>? subchannels, ushort? requiredSubchannel)
    {
        await using (await closeLock.ReadLockAsync())
        {
            if (closed)
            {
                throw new InvalidStateException("DataProducer closed");
            }

            var sendNotification = new SendNotificationT
            {
                Ppid               = ppid,
                Data               = data.ToList(),
                Subchannels        = subchannels ?? [],
                RequiredSubchannel = requiredSubchannel
            };

            // Fire and forget
            channel.NotifyAsync(
                bufferBuilder => SendNotification.Pack(bufferBuilder, sendNotification).Value,
                Event.PRODUCER_SEND,
                Antelcat.MediasoupSharp.FBS.Notification.Body.DataProducer_SendNotification,
                @internal.DataProducerId
            ).ContinueWithOnFaultedHandleLog(logger);
        }
    }

    #region Event Handlers

    private static void HandleWorkerNotifications()
    {
        // No need to subscribe to any event.
    }

    private void HandleListenerError() =>
        this.On(static x => x.ListenerError, tuple =>
        {
            logger.LogError(tuple.error,
                "event listener threw an error [eventName:{EventName}]:",
                tuple.eventName);
        });

    #endregion Event Handlers
}