using Antelcat.AutoGen.ComponentModel.Diagnostic;
using Antelcat.MediasoupSharp.Internals.Extensions;
using FBS.Notification;
using FBS.Producer;
using FBS.Request;
using FBS.RtpParameters;
using Google.FlatBuffers;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Threading;
using Type = FBS.RtpParameters.Type;

namespace Antelcat.MediasoupSharp;

using ProducerObserver = EnhancedEventEmitter<ProducerObserverEvents>;

public class ProducerOptions<TProducerAppData>
{
    /// <summary>
    /// Producer id (just for Router.pipeToRouter() method).
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// Media kind ('audio' or 'video').
    /// </summary>
    public MediaKind Kind { get; set; }

    /// <summary>
    /// RTP parameters defining what the endpoint is sending.
    /// </summary>
    public required RtpParameters RtpParameters { get; set; }

    /// <summary>
    /// Whether the producer must start in paused mode. Default false.
    /// </summary>
    public bool Paused { get; set; }

    /// <summary>
    /// Just for video. Time (in ms) before asking the sender for a new key frame
    /// after having asked a previous one. Default 0.
    /// </summary>
    public uint KeyFrameRequestDelay { get; set; }

    /// <summary>
    /// Custom application data.
    /// </summary>
    public TProducerAppData? AppData { get; set; }
}

public abstract class ProducerEvents
{
    public          object?                             TransportClose;
    public required List<ScoreT>                        Score;
    public required VideoOrientationChangeNotificationT VideoOrientationChange;
    public required TraceNotificationT                  Trace;

    public (string, Exception)? ListenerError;

    // Private events.
    internal object? close;
}

public abstract class ProducerObserverEvents
{
    public object?                              Close;
    public object?                              Pause;
    public object?                              Resume;
    public List<ScoreT>?                        Score;
    public VideoOrientationChangeNotificationT? VideoOrientationChange;
    public TraceNotificationT?                  Trace;
}

public class ProducerInternal : TransportInternal
{
    /// <summary>
    /// Producer id.
    /// </summary>
    public required string ProducerId { get; init; }
}

public class ProducerData
{
    /// <summary>
    /// Media kind.
    /// </summary>
    public MediaKind Kind { get; init; }

    /// <summary>
    /// RTP parameters.
    /// </summary>
    public required RtpParameters RtpParameters { get; init; }

    /// <summary>
    /// Producer type.
    /// </summary>
    public Type Type { get; init; }

    /// <summary>
    /// Consumable RTP parameters.
    /// </summary>
    public required RtpParameters ConsumableRtpParameters { get; init; }
}

[AutoExtractInterface]
public class Producer<TProducerAppData> : EnhancedEventEmitter<ProducerEvents>, IProducer
    where TProducerAppData : new()
{
    /// <summary>
    /// Logger
    /// </summary>
    private readonly ILogger logger = new Logger<Producer<TProducerAppData>>();

    /// <summary>
    /// Producer id.
    /// </summary>
    public string Id => @internal.ProducerId;

    /// <summary>
    /// Whether the Producer is closed.
    /// </summary>
    public bool Closed { get; private set; }

    public FBS.RtpParameters.MediaKind Kind => Data.Kind;

    private readonly AsyncReaderWriterLock closeLock = new();

    /// <summary>
    /// Paused flag.
    /// </summary>
    public bool Paused { get; private set; }

    private readonly AsyncAutoResetEvent pauseLock = new();

    /// <summary>
    /// Internal data.
    /// </summary>
    private readonly ProducerInternal @internal;

    private readonly bool isCheckConsumer = false;

    private readonly Timer? checkConsumersTimer;

#if DEBUG
    private const int CheckConsumersTimeSeconds = 60 * 60 * 24;
#else
        private const int CheckConsumersTimeSeconds = 10;
#endif

    /// <summary>
    /// Producer data.
    /// </summary>
    public ProducerData Data { get; set; }

    /// <summary>
    /// Channel instance.
    /// </summary>
    private readonly IChannel channel;

    /// <summary>
    /// App custom data.
    /// </summary>
    public TProducerAppData AppData { get; }

    /// <summary>
    /// [扩展]Consumers
    /// </summary>
    private readonly Dictionary<string, IConsumer> consumers = new();

    /// <summary>
    /// [扩展]Source.
    /// </summary>
    public string? Source { get; set; }

    /// <summary>
    /// Current score.
    /// </summary>
    public List<ScoreT> Score = [];

    /// <summary>
    /// Observer instance.
    /// </summary>
    public ProducerObserver Observer { get; } = new();

    /// <summary>
    /// <para>Events:</para>
    /// <para>@emits <see cref="ProducerEvents.TransportClose"/></para>
    /// <para>@emits <see cref="ProducerEvents.Score"/> - (score: ProducerScore[])</para>
    /// <para>@emits <see cref="ProducerEvents.VideoOrientationChange"/> - (videoOrientation: ProducerVideoOrientation)</para>
    /// <para>@emits <see cref="ProducerEvents.Trace"/> - (trace: ProducerTraceEventData)</para>
    /// <para>@emits <see cref="ProducerEvents.close"/></para>
    /// <para>Observer events:</para>
    /// <para>@emits <see cref="ProducerObserverEvents.Close"/></para>
    /// <para>@emits <see cref="ProducerObserverEvents.Pause"/></para>
    /// <para>@emits <see cref="ProducerObserverEvents.Resume"/></para>
    /// <para>@emits <see cref="ProducerObserverEvents.Score"/> - (score: ProducerScore[])</para>
    /// <para>@emits <see cref="ProducerObserverEvents.VideoOrientationChange"/> - (videoOrientation: ProducerVideoOrientation)</para>
    /// <para>@emits <see cref="ProducerObserverEvents.Trace"/> - (trace: ProducerTraceEventData)</para>
    /// </summary>
    public Producer(
        ProducerInternal @internal,
        ProducerData data,
        IChannel channel,
        TProducerAppData? appData,
        bool paused
    )
    {
        this.@internal = @internal;
        Data           = data;
        this.channel   = channel;
        AppData        = appData ?? new ();
        Paused         = paused;
        pauseLock.Set();

        if (isCheckConsumer)
        {
            checkConsumersTimer = new Timer(
                CheckConsumers,
                null,
                TimeSpan.FromSeconds(CheckConsumersTimeSeconds),
                TimeSpan.FromMilliseconds(-1)
            );
        }

        HandleWorkerNotifications();
    }

    /// <summary>
    /// Close the Producer.
    /// </summary>
    public async Task CloseAsync()
    {
        logger.LogDebug("CloseAsync() | Producer:{ProducerId}", Id);

        await using (await closeLock.WriteLockAsync())
        {
            CloseInternal();
        }
    }

    private void CloseInternal()
    {
        if (Closed)
        {
            return;
        }

        Closed = true;

        checkConsumersTimer?.Dispose();

        // Remove notification subscriptions.
        channel.OnNotification -= OnNotificationHandle;

        // Build Request
        var bufferBuilder = channel.BufferPool.Get();

        var requestOffset = FBS.Transport.CloseProducerRequest.Pack(bufferBuilder,
            new FBS.Transport.CloseProducerRequestT
            {
                ProducerId = @internal.ProducerId
            });

        // Fire and forget
        channel.RequestAsync(bufferBuilder, Method.TRANSPORT_CLOSE_CONSUMER,
                FBS.Request.Body.Transport_CloseConsumerRequest,
                requestOffset.Value,
                @internal.TransportId
            )
            .ContinueWithOnFaultedHandleLog(logger);

        this.Emit(static x => x.close);

        // Emit observer event.
        Observer.Emit(static x=>x.Close);
    }

    /// <summary>
    /// Transport was closed.
    /// </summary>
    public async Task TransportClosedAsync()
    {
        logger.LogDebug("TransportClosedAsync() | Producer:{ProducerId}", Id);

        await using (await closeLock.WriteLockAsync())
        {
            if (Closed) return;

            Closed = true;

            if (checkConsumersTimer != null) await checkConsumersTimer.DisposeAsync();

            // Remove notification subscriptions.
            channel.OnNotification -= OnNotificationHandle;

            this.Emit(static x=>x.TransportClose);

            // Emit observer event.
            Observer.Emit(static x=>x.Close);
        }
    }

    /// <summary>
    /// Dump DataProducer.
    /// </summary>
    public async Task<FBS.Producer.DumpResponseT> DumpAsync()
    {
        logger.LogDebug("DumpAsync() | Producer:{ProducerId}", Id);

        await using (await closeLock.ReadLockAsync())
        {
            if (Closed) throw new InvalidStateException("Producer closed");

            // Build Request
            var bufferBuilder = channel.BufferPool.Get();

            var response =
                await channel.RequestAsync(bufferBuilder, Method.PRODUCER_DUMP, null, null, @internal.ProducerId);
            var data = response.NotNull().BodyAsProducer_DumpResponse().UnPack();

            return data;
        }
    }

    /// <summary>
    /// Get DataProducer stats.
    /// </summary>
    public async Task<List<FBS.RtpStream.StatsT>> GetStatsAsync()
    {
        logger.LogDebug("GetStatsAsync() | Producer:{ProducerId}", Id);

        await using (await closeLock.ReadLockAsync())
        {
            if (Closed) throw new InvalidStateException("Producer closed");

            // Build Request
            var bufferBuilder = channel.BufferPool.Get();

            var response = await channel.RequestAsync(bufferBuilder, Method.PRODUCER_GET_STATS, null, null,
                @internal.ProducerId);
            var stats = response.NotNull().BodyAsProducer_GetStatsResponse().UnPack().Stats;

            return stats;
        }
    }

    /// <summary>
    /// Pause the Producer.
    /// </summary>
    public async Task PauseAsync()
    {
        logger.LogDebug("PauseAsync() | Producer:{ProducerId}", Id);

        await using (await closeLock.ReadLockAsync())
        {
            if (Closed)
            {
                throw new InvalidStateException("Producer closed");
            }

            await pauseLock.WaitAsync();
            try
            {
                var wasPaused = Paused;

                // Build Request
                var bufferBuilder = channel.BufferPool.Get();

                await channel.RequestAsync(bufferBuilder, Method.PRODUCER_PAUSE, null, null, @internal.ProducerId);

                Paused = true;

                // Emit observer event.
                if (!wasPaused)
                {
                    Observer.Emit(static x=>x.Pause);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "PauseAsync()");
            }
            finally
            {
                pauseLock.Set();
            }
        }
    }

    /// <summary>
    /// Resume the Producer.
    /// </summary>
    public async Task ResumeAsync()
    {
        logger.LogDebug("ResumeAsync() | Producer:{ProducerId}", Id);

        await using (await closeLock.ReadLockAsync())
        {
            if (Closed)
            {
                throw new InvalidStateException("Producer closed");
            }

            await pauseLock.WaitAsync();
            try
            {
                var wasPaused = Paused;

                // Build Request
                var bufferBuilder = channel.BufferPool.Get();

                await channel.RequestAsync(bufferBuilder, Method.PRODUCER_RESUME, null, null, @internal.ProducerId);

                Paused = false;

                // Emit observer event.
                if (wasPaused)
                {
                    Observer.Emit(static x=>x.Resume);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "ResumeAsync()");
            }
            finally
            {
                pauseLock.Set();
            }
        }
    }

    /// <summary>
    /// Enable 'trace' event.
    /// </summary>
    public async Task EnableTraceEventAsync(List<FBS.Producer.TraceEventType> types)
    {
        logger.LogDebug("EnableTraceEventAsync() | Producer:{ProducerId}", Id);

        await using (await closeLock.ReadLockAsync())
        {
            if (Closed)
            {
                throw new InvalidStateException("Producer closed");
            }

            // Build Request
            var bufferBuilder = channel.BufferPool.Get();

            var requestOffset = EnableTraceEventRequest.Pack(bufferBuilder, new EnableTraceEventRequestT
            {
                Events = types ?? []
            });

            // Fire and forget
            channel.RequestAsync(bufferBuilder, Method.CONSUMER_ENABLE_TRACE_EVENT,
                    FBS.Request.Body.Consumer_EnableTraceEventRequest,
                    requestOffset.Value,
                    @internal.ProducerId)
                .ContinueWithOnFaultedHandleLog(logger);
        }
    }

    /// <summary>
    /// Send RTP packet (just valid for Producers created on a DirectTransport).
    /// </summary>
    public async Task SendAsync(byte[] rtpPacket)
    {
        await using (await closeLock.ReadLockAsync())
        {
            if (Closed)
            {
                throw new InvalidStateException("Producer closed");
            }

            // Build Request
            var bufferBuilder = new FlatBufferBuilder(1024 + rtpPacket.Length);

            var dataOffset = SendNotification.CreateDataVectorBlock(
                bufferBuilder,
                rtpPacket
            );

            var notificationOffset = SendNotification.CreateSendNotification(bufferBuilder, dataOffset);

            // Fire and forget
            channel.NotifyAsync(bufferBuilder, Event.PRODUCER_SEND,
                FBS.Notification.Body.Producer_SendNotification,
                notificationOffset.Value,
                @internal.ProducerId
            ).ContinueWithOnFaultedHandleLog(logger);
        }
    }

    public async Task AddConsumerAsync(IConsumer consumer)
    {
        await using (await closeLock.ReadLockAsync())
        {
            if (Closed)
            {
                throw new InvalidStateException("Producer closed");
            }

            consumers[consumer.Id] = consumer;
        }
    }

    public async Task RemoveConsumerAsync(string consumerId)
    {
        logger.LogDebug("RemoveConsumer() | Producer:{ProducerId} ConsumerId:{ConsumerId}", Id, consumerId);

        await using (await closeLock.ReadLockAsync())
        {
            // 关闭后也允许移除
            consumers.Remove(consumerId);
        }
    }

    #region Event Handlers

    private void HandleWorkerNotifications()
    {
        channel.OnNotification += OnNotificationHandle;
    }

    private void OnNotificationHandle(string handlerId, Event @event, Notification notification)
    {
        if (handlerId != Id)
        {
            return;
        }

        switch (@event)
        {
            case Event.PRODUCER_SCORE:
            {
                var scoreNotification = notification.BodyAsProducer_ScoreNotification();
                var score             = scoreNotification.UnPack().Scores;
                Score = score;

                this.Emit(static x => x.Score, score);

                // Emit observer event.
                Observer.Emit(static x=> x.Score, score);

                break;
            }
            case Event.PRODUCER_VIDEO_ORIENTATION_CHANGE:
            {
                var videoOrientationChangeNotification =
                    notification.BodyAsProducer_VideoOrientationChangeNotification();
                var videoOrientation = videoOrientationChangeNotification.UnPack();

                this.Emit(static x=>x.VideoOrientationChange, videoOrientation);

                // Emit observer event.
                Observer.Emit(static x=>x.VideoOrientationChange, videoOrientation);

                break;
            }
            case Event.PRODUCER_TRACE:
            {
                var traceNotification = notification.BodyAsProducer_TraceNotification();
                var trace             = traceNotification.UnPack();

                this.Emit(static x => x.Trace, trace);

                // Emit observer event.
                Observer.Emit(static x => x.Trace, trace);

                break;
            }
            default:
            {
                logger.LogError("OnNotificationHandle() | Ignoring unknown event: {Event}", @event);
                break;
            }
        }
    }

    #endregion Event Handlers

    #region Private Methods

#pragma warning disable VSTHRD100 // Avoid async void methods
    private async void CheckConsumers(object? state)
#pragma warning restore VSTHRD100 // Avoid async void methods
    {
        logger.LogDebug("CheckConsumer() | Producer:{ProducerId} ConsumerCount:{Count}", @internal.ProducerId,
            consumers.Count);

        // NOTE: 使用写锁
        await using (await closeLock.WriteLockAsync())
        {
            if (Closed)
            {
                checkConsumersTimer?.Dispose();
                return;
            }

            if (consumers.Count == 0)
            {
                CloseInternal();
                checkConsumersTimer?.Dispose();
            }
            else
            {
                checkConsumersTimer?.Change(
                    TimeSpan.FromSeconds(CheckConsumersTimeSeconds),
                    TimeSpan.FromMilliseconds(-1)
                );
            }
        }
    }

    #endregion Private Methods
}