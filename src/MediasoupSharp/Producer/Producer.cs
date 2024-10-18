using FBS.Notification;
using FBS.Producer;
using FBS.Request;
using FBS.RtpStream;
using Google.FlatBuffers;
using MediasoupSharp.Channel;
using MediasoupSharp.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Threading;

namespace MediasoupSharp.Producer;

public class Producer : EventEmitter.EventEmitter
{
    /// <summary>
    /// Logger
    /// </summary>
    private readonly ILogger<Producer> logger;

    /// <summary>
    /// Whether the Producer is closed.
    /// </summary>
    public bool Closed { get; private set; }

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
    /// Producer id.
    /// </summary>
    public string ProducerId => @internal.ProducerId;

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
    public Dictionary<string, object> AppData { get; }

    /// <summary>
    /// [扩展]Consumers
    /// </summary>
    private readonly Dictionary<string, Consumer.Consumer> consumers = new();

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
    public EventEmitter.EventEmitter Observer { get; } = new();

    /// <summary>
    /// <para>Events:</para>
    /// <para>@emits transportclose</para></para>
    /// <para>@emits score - (score: ProducerScore[])</para>
    /// <para>@emits videoorientationchange - (videoOrientation: ProducerVideoOrientation)</para>
    /// <para>@emits trace - (trace: ProducerTraceEventData)</para>
    /// <para>@emits @close</para>
    /// <para>Observer events:</para>
    /// <para>@emits close</para>
    /// <para>@emits pause</para>
    /// <para>@emits resume</para>
    /// <para>@emits score - (score: ProducerScore[])</para>
    /// <para>@emits videoorientationchange - (videoOrientation: ProducerVideoOrientation)</para>
    /// <para>@emits trace - (trace: ProducerTraceEventData)</para>
    /// </summary>
    public Producer(
        ILoggerFactory loggerFactory,
        ProducerInternal @internal,
        ProducerData data,
        IChannel channel,
        Dictionary<string, object>? appData,
        bool paused
    )
    {
        logger = loggerFactory.CreateLogger<Producer>();

        this.@internal = @internal;
        Data           = data;
        this.channel   = channel;
        AppData        = appData ?? new Dictionary<string, object>();
        Paused         = paused;
        pauseLock.Set();

        if(isCheckConsumer)
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
        logger.LogDebug("CloseAsync() | Producer:{ProducerId}", ProducerId);

        await using(await closeLock.WriteLockAsync())
        {
            CloseInternal();
        }
    }

    private void CloseInternal()
    {
        if(Closed)
        {
            return;
        }

        Closed = true;

        checkConsumersTimer?.Dispose();

        // Remove notification subscriptions.
        channel.OnNotification -= OnNotificationHandle;

        // Build Request
        var bufferBuilder = channel.BufferPool.Get();

        var requestOffset = FBS.Transport.CloseProducerRequest.Pack(bufferBuilder, new FBS.Transport.CloseProducerRequestT
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

        Emit("@close");

        // Emit observer event.
        Observer.Emit("close");
    }

    /// <summary>
    /// Transport was closed.
    /// </summary>
    public async Task TransportClosedAsync()
    {
        logger.LogDebug("TransportClosedAsync() | Producer:{ProducerId}", ProducerId);

        await using(await closeLock.WriteLockAsync())
        {
            if(Closed) return;

            Closed = true;

            if(checkConsumersTimer != null) await checkConsumersTimer.DisposeAsync();

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
        logger.LogDebug("DumpAsync() | Producer:{ProducerId}", ProducerId);

        await using(await closeLock.ReadLockAsync())
        {
            if (Closed) throw new InvalidStateException("Producer closed");

            // Build Request
            var bufferBuilder = channel.BufferPool.Get();

            var response = await channel.RequestAsync(bufferBuilder, Method.PRODUCER_DUMP, null, null, @internal.ProducerId);
            var data = response.Value.BodyAsProducer_DumpResponse().UnPack();

            return data;
        }
    }

    /// <summary>
    /// Get DataProducer stats.
    /// </summary>
    public async Task<List<StatsT>> GetStatsAsync()
    {
        logger.LogDebug("GetStatsAsync() | Producer:{ProducerId}", ProducerId);

        await using(await closeLock.ReadLockAsync())
        {
            if(Closed) throw new InvalidStateException("Producer closed");

            // Build Request
            var bufferBuilder = channel.BufferPool.Get();

            var response = await channel.RequestAsync(bufferBuilder, Method.PRODUCER_GET_STATS, null, null, @internal.ProducerId);
            var stats = response.Value.BodyAsProducer_GetStatsResponse().UnPack().Stats;

            return stats;
        }
    }

    /// <summary>
    /// Pause the Producer.
    /// </summary>
    public async Task PauseAsync()
    {
        logger.LogDebug("PauseAsync() | Producer:{ProducerId}", ProducerId);

        await using(await closeLock.ReadLockAsync())
        {
            if(Closed)
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
    /// Resume the Producer.
    /// </summary>
    public async Task ResumeAsync()
    {
        logger.LogDebug("ResumeAsync() | Producer:{ProducerId}", ProducerId);

        await using(await closeLock.ReadLockAsync())
        {
            if(Closed)
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
                if(wasPaused)
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
    /// Enable 'trace' event.
    /// </summary>
    public async Task EnableTraceEventAsync(List<TraceEventType> types)
    {
        logger.LogDebug("EnableTraceEventAsync() | Producer:{ProducerId}", ProducerId);

        await using(await closeLock.ReadLockAsync())
        {
            if(Closed)
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
        await using(await closeLock.ReadLockAsync())
        {
            if(Closed)
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

    public async Task AddConsumerAsync(Consumer.Consumer consumer)
    {
        await using(await closeLock.ReadLockAsync())
        {
            if(Closed)
            {
                throw new InvalidStateException("Producer closed");
            }

            consumers[consumer.ConsumerId] = consumer;
        }
    }

    public async Task RemoveConsumerAsync(string consumerId)
    {
        logger.LogDebug("RemoveConsumer() | Producer:{ProducerId} ConsumerId:{ConsumerId}", ProducerId, consumerId);

        await using(await closeLock.ReadLockAsync())
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
        if(handlerId != ProducerId)
        {
            return;
        }

        switch(@event)
        {
            case Event.PRODUCER_SCORE:
            {
                var scoreNotification = notification.BodyAsProducer_ScoreNotification();
                var score             = scoreNotification.UnPack().Scores;
                Score = score;

                Emit("score", score);

                // Emit observer event.
                Observer.Emit("score", score);

                break;
            }
            case Event.PRODUCER_VIDEO_ORIENTATION_CHANGE:
            {
                var videoOrientationChangeNotification = notification.BodyAsProducer_VideoOrientationChangeNotification();
                var videoOrientation = videoOrientationChangeNotification.UnPack();

                Emit("videoorientationchange", videoOrientation);

                // Emit observer event.
                Observer.Emit("videoorientationchange", videoOrientation);

                break;
            }
            case Event.PRODUCER_TRACE:
            {
                var traceNotification = notification.BodyAsProducer_TraceNotification();
                var trace             = traceNotification.UnPack();

                Emit("trace", trace);

                // Emit observer event.
                Observer.Emit("trace", trace);

                break;
            }
            default:
            {
                logger.LogError("OnNotificationHandle() | Ignoring unknown event: {@event}", @event);
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
        logger.LogDebug("CheckConsumer() | Producer:{ProducerId} ConsumerCount:{Count}", @internal.ProducerId, consumers.Count);

        // NOTE: 使用写锁
        await using(await closeLock.WriteLockAsync())
        {
            if(Closed)
            {
                checkConsumersTimer?.Dispose();
                return;
            }

            if(consumers.Count == 0)
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