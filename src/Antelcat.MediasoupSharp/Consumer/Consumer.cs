using Antelcat.MediasoupSharp.Channel;
using Antelcat.MediasoupSharp.Exceptions;
using FBS.Consumer;
using FBS.Notification;
using FBS.Request;
using FBS.RtpStream;
using Antelcat.MediasoupSharp.Internals.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Threading;

namespace Antelcat.MediasoupSharp.Consumer;

public class Consumer : EnhancedEvent.EnhancedEventEmitter
{
    /// <summary>
    /// Logger.
    /// </summary>
    private readonly ILogger<Consumer> logger;

    /// <summary>
    /// Whether the Consumer is closed.
    /// </summary>
    private bool closed;

    private readonly AsyncReaderWriterLock closeLock = new();

    /// <summary>
    /// Paused flag.
    /// </summary>
    private bool paused;

    private readonly AsyncAutoResetEvent pauseLock = new();

    /// <summary>
    /// Internal data.
    /// </summary>
    private readonly ConsumerInternal @internal;

    /// <summary>
    /// Consumer id.
    /// </summary>
    public string Id => @internal.ConsumerId;

    /// <summary>
    /// Consumer data.
    /// </summary>
    public ConsumerData Data { get; set; }

    /// <summary>
    /// Producer id.
    /// </summary>
    public string ProducerId => Data.ProducerId;

    /// <summary>
    /// Channel instance.
    /// </summary>
    private readonly IChannel channel;

    /// <summary>
    /// App custom data.
    /// </summary>
    public AppData AppData { get; }

    /// <summary>
    /// [扩展]Source.
    /// </summary>
    public string? Source { get; set; }

    /// <summary>
    /// Whether the associate Producer is paused.
    /// </summary>
    public bool ProducerPaused { get; private set; }

    /// <summary>
    /// Current priority.
    /// </summary>
    public int Priority { get; private set; } = 1;

    /// <summary>
    /// Current score.
    /// </summary>
    public ConsumerScoreT? Score;

    /// <summary>
    /// Preferred layers.
    /// </summary>
    public ConsumerLayersT? PreferredLayers { get; private set; }

    /// <summary>
    /// Curent layers.
    /// </summary>
    public ConsumerLayersT? CurrentLayers { get; private set; }

    /// <summary>
    /// Observer instance.
    /// </summary>
    public EnhancedEvent.EnhancedEventEmitter Observer { get; } = new();

    /// <summary>
    /// <para>Events:</para>
    /// <para>@emits transportclose</para>
    /// <para>@emits producerclose</para>
    /// <para>@emits producerpause</para>
    /// <para>@emits producerresume</para>
    /// <para>@emits score - (score: ConsumerScore)</para>
    /// <para>@emits layerschange - (layers: ConsumerLayers | undefined)</para>
    /// <para>@emits trace - (trace: ConsumerTraceEventData)</para>
    /// <para>@emits @close</para>
    /// <para>@emits @producerclose</para>
    /// <para>Observer events:</para>
    /// <para>@emits close</para>
    /// <para>@emits pause</para>
    /// <para>@emits resume</para>
    /// <para>@emits score - (score: ConsumerScore)</para>
    /// <para>@emits layerschange - (layers: ConsumerLayers | undefined)</para>
    /// <para>@emits rtp - (packet: Buffer)</para>
    /// <para>@emits trace - (trace: ConsumerTraceEventData)</para>
    /// </summary>
    public Consumer(
        ILoggerFactory loggerFactory,
        ConsumerInternal @internal,
        ConsumerData data,
        IChannel channel,
        AppData? appData,
        bool paused,
        bool producerPaused,
        ConsumerScoreT? score,
        ConsumerLayersT? preferredLayers
    )
    {
        logger = loggerFactory.CreateLogger<Consumer>();

        this.@internal  = @internal;
        Data            = data;
        this.channel    = channel;
        AppData         = appData ?? new Dictionary<string, object>();
        this.paused     = paused;
        ProducerPaused  = producerPaused;
        Score           = score;
        PreferredLayers = preferredLayers;
        pauseLock.Set();

        HandleWorkerNotifications();
    }

    /// <summary>
    /// Close the Producer.
    /// </summary>
    public async Task CloseAsync()
    {
        logger.LogDebug("CloseAsync() | Consumer:{ConsumerId}", Id);

        await using(await closeLock.WriteLockAsync())
        {
            if(closed)
            {
                return;
            }

            closed = true;

            // Remove notification subscriptions.
            channel.OnNotification -= OnNotificationHandle;

            // Build Request
            var bufferBuilder = channel.BufferPool.Get();

            var requestOffset = FBS.Transport.CloseConsumerRequest.Pack(bufferBuilder, new FBS.Transport.CloseConsumerRequestT
            {
                ConsumerId = @internal.ConsumerId
            });

            // Fire and forget
            channel.RequestAsync(
                    bufferBuilder,
                    Method.TRANSPORT_CLOSE_CONSUMER,
                    FBS.Request.Body.Transport_CloseConsumerRequest,
                    requestOffset.Value,
                    @internal.TransportId
                )
                .ContinueWithOnFaultedHandleLog(logger);

            Emit("@close");

            // Emit observer event.
            Observer.Emit("close");
        }
    }

    /// <summary>
    /// Transport was closed.
    /// </summary>
    public async Task TransportClosedAsync()
    {
        logger.LogDebug("TransportClosed() | Consumer:{ConsumerId}", Id);

        await using(await closeLock.WriteLockAsync())
        {
            if(closed)
            {
                return;
            }

            closed = true;

            // Remove notification subscriptions.
            channel.OnNotification -= OnNotificationHandle;

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
        logger.LogDebug("DumpAsync() | Consumer:{ConsumerId}", Id);

        await using(await closeLock.ReadLockAsync())
        {
            if(closed)
            {
                throw new InvalidStateException("Consumer closed");
            }

            var bufferBuilder = channel.BufferPool.Get();
            var response = await channel.RequestAsync(bufferBuilder, Method.CONSUMER_DUMP, null, null, @internal.ConsumerId);
            var data = response.Value.BodyAsConsumer_DumpResponse().UnPack();
            return data;
        }
    }

    /// <summary>
    /// Get DataProducer stats.
    /// </summary>
    public async Task<List<StatsT>> GetStatsAsync()
    {
        logger.LogDebug("GetStatsAsync() | Consumer:{ConsumerId}", Id);

        await using(await closeLock.ReadLockAsync())
        {
            if(closed)
            {
                throw new InvalidStateException("Consumer closed");
            }

            var bufferBuilder = channel.BufferPool.Get();
            var response = await channel.RequestAsync(bufferBuilder, Method.CONSUMER_GET_STATS, null, null, @internal.ConsumerId);
            var stats = response?.BodyAsConsumer_GetStatsResponse().UnPack().Stats;
            return stats;
        }
    }

    /// <summary>
    /// Pause the Consumer.
    /// </summary>
    public async Task PauseAsync()
    {
        logger.LogDebug("PauseAsync() | Consumer:{ConsumerId}", Id);

        await using(await closeLock.ReadLockAsync())
        {
            if(closed)
            {
                throw new InvalidStateException("Consumer closed");
            }

            await pauseLock.WaitAsync();
            try
            {
                var wasPaused = paused || ProducerPaused;

                var bufferBuilder = channel.BufferPool.Get();

                // Fire and forget
                channel.RequestAsync(bufferBuilder, Method.CONSUMER_PAUSE, null, null, @internal.ConsumerId)
                    .ContinueWithOnFaultedHandleLog(logger);

                paused = true;

                // Emit observer event.
                if(!wasPaused)
                {
                    Observer.Emit("pause");
                }
            }
            catch(Exception ex)
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
    /// Resume the Consumer.
    /// </summary>
    public async Task ResumeAsync()
    {
        logger.LogDebug("ResumeAsync() | Consumer:{ConsumerId}", Id);

        await using(await closeLock.ReadLockAsync())
        {
            if(closed)
            {
                throw new InvalidStateException("Consumer closed");
            }

            await pauseLock.WaitAsync();
            try
            {
                var wasPaused = paused || ProducerPaused;

                var bufferBuilder = channel.BufferPool.Get();

                // Fire and forget
                channel.RequestAsync(bufferBuilder, Method.CONSUMER_RESUME, null, null, @internal.ConsumerId)
                    .ContinueWithOnFaultedHandleLog(logger);

                paused = false;

                // Emit observer event.
                if(wasPaused && !ProducerPaused)
                {
                    Observer.Emit("resume");
                }
            }
            catch(Exception ex)
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
    /// Set preferred video layers.
    /// </summary>
    public async Task SetPreferredLayersAsync(SetPreferredLayersRequestT setPreferredLayersRequest)
    {
        logger.LogDebug("SetPreferredLayersAsync() | Consumer:{ConsumerId}", Id);

        await using(await closeLock.ReadLockAsync())
        {
            if(closed)
            {
                throw new InvalidStateException("Consumer closed");
            }

            // Build Request
            var bufferBuilder = channel.BufferPool.Get();

            var setPreferredLayersRequestOffset = SetPreferredLayersRequest.Pack(bufferBuilder, setPreferredLayersRequest);

            var response = await channel.RequestAsync(
                bufferBuilder,
                Method.CONSUMER_SET_PREFERRED_LAYERS,
                FBS.Request.Body.Consumer_SetPreferredLayersRequest,
                setPreferredLayersRequestOffset.Value,
                @internal.ConsumerId);
            var preferredLayers = response?.BodyAsConsumer_SetPreferredLayersResponse().UnPack().PreferredLayers;

            PreferredLayers = preferredLayers;
        }
    }

    /// <summary>
    /// Set priority.
    /// </summary>
    public async Task SetPriorityAsync(SetPriorityRequestT setPriorityRequest)
    {
        logger.LogDebug("SetPriorityAsync() | Consumer:{ConsumerId}", Id);

        await using(await closeLock.ReadLockAsync())
        {
            if(closed)
            {
                throw new InvalidStateException("Consumer closed");
            }

            var bufferBuilder = channel.BufferPool.Get();

            var setPriorityRequestOffset = SetPriorityRequest.Pack(bufferBuilder, setPriorityRequest);

            var response = await channel.RequestAsync(
                bufferBuilder,
                Method.CONSUMER_SET_PRIORITY,
                FBS.Request.Body.Consumer_SetPriorityRequest,
                setPriorityRequestOffset.Value,
                @internal.ConsumerId);

            var priorityResponse = response.Value.BodyAsConsumer_SetPriorityResponse().UnPack().Priority;

            Priority = priorityResponse;
        }
    }

    /// <summary>
    /// Unset priority.
    /// </summary>
    public Task UnsetPriorityAsync()
    {
        logger.LogDebug("UnsetPriorityAsync() | Consumer:{ConsumerId}", Id);

        return SetPriorityAsync(new SetPriorityRequestT
        {
            Priority = 1,
        });
    }

    /// <summary>
    /// Request a key frame to the Producer.
    /// </summary>
    public async Task RequestKeyFrameAsync()
    {
        logger.LogDebug("RequestKeyFrameAsync() | Consumer:{ConsumerId}", Id);

        await using(await closeLock.ReadLockAsync())
        {
            if(closed)
            {
                throw new InvalidStateException("Consumer closed");
            }

            // Build Request
            var bufferBuilder = channel.BufferPool.Get();

            await channel.RequestAsync(bufferBuilder, Method.CONSUMER_REQUEST_KEY_FRAME,
                null,
                null,
                @internal.ConsumerId);
        }
    }

    /// <summary>
    /// Enable 'trace' event.
    /// </summary>
    public async Task EnableTraceEventAsync(List<TraceEventType> types)
    {
        logger.LogDebug("EnableTraceEventAsync() | Consumer:{ConsumerId}", Id);

        await using(await closeLock.ReadLockAsync())
        {
            if(closed)
            {
                throw new InvalidStateException("Consumer closed");
            }

            // Build Request
            var bufferBuilder = channel.BufferPool.Get();

            var request = new EnableTraceEventRequestT
            {
                Events = types ?? []
            };

            var requestOffset = EnableTraceEventRequest.Pack(bufferBuilder, request);

            // Fire and forget
            channel.RequestAsync(
                    bufferBuilder,
                    Method.CONSUMER_ENABLE_TRACE_EVENT,
                    FBS.Request.Body.Consumer_EnableTraceEventRequest,
                    requestOffset.Value,
                    @internal.ConsumerId)
                .ContinueWithOnFaultedHandleLog(logger);
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
        if(handlerId != Id)
        {
            return;
        }

        switch(@event)
        {
            case Event.CONSUMER_PRODUCER_CLOSE:
            {
                await using(await closeLock.WriteLockAsync())
                {
                    if(closed)
                    {
                        break;
                    }

                    closed = true;

                    // Remove notification subscriptions.
                    channel.OnNotification -= OnNotificationHandle;

                    Emit("@producerclose");
                    Emit("producerclose");

                    // Emit observer event.
                    Observer.Emit("close");
                }

                break;
            }
            case Event.CONSUMER_PRODUCER_PAUSE:
            {
                if(ProducerPaused)
                {
                    break;
                }

                var wasPaused = paused || ProducerPaused;

                ProducerPaused = true;

                Emit("producerpause");

                // Emit observer event.
                if(!wasPaused)
                {
                    Observer.Emit("pause");
                }

                break;
            }
            case Event.CONSUMER_PRODUCER_RESUME:
            {
                if(!ProducerPaused)
                {
                    break;
                }

                var wasPaused = paused || ProducerPaused;

                ProducerPaused = false;

                Emit("producerresume");

                // Emit observer event.
                if(wasPaused && !paused)
                {
                    Observer.Emit("resume");
                }

                break;
            }
            case Event.CONSUMER_SCORE:
            {
                var scoreNotification = notification.BodyAsConsumer_ScoreNotification();
                var score             = scoreNotification.Score!.Value.UnPack();
                Score = score;

                Emit(nameof(score), Score);

                // Emit observer event.
                Observer.Emit(nameof(score), Score);

                break;
            }
            case Event.CONSUMER_LAYERS_CHANGE:
            {
                var layersChangeNotification = notification.BodyAsConsumer_LayersChangeNotification();
                var currentLayers            = layersChangeNotification.Layers!.Value.UnPack();
                CurrentLayers = currentLayers;

                Emit("layerschange", CurrentLayers);

                // Emit observer event.
                Observer.Emit("layersChange", CurrentLayers);

                break;
            }
            case Event.CONSUMER_TRACE:
            {
                var traceNotification = notification.BodyAsConsumer_TraceNotification();
                var trace             = traceNotification.UnPack();

                Emit(nameof(trace), trace);

                // Emit observer event.
                Observer.Emit(nameof(trace), trace);

                break;
            }
            default:
            {
                logger.LogError("OnNotificationHandle() | Ignoring unknown event{@event}", @event);
                break;
            }
        }
    }
}

#endregion Event Handlers