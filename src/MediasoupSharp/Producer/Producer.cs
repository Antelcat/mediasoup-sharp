using MediasoupSharp.Channel;
using MediasoupSharp.Exceptions;
using MediasoupSharp.PayloadChannel;

namespace MediasoupSharp.Producer
{
    public class Producer : EventEmitter
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
        /// Channel instance.
        /// </summary>
        private readonly IPayloadChannel payloadChannel;

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
        public ProducerScore[] Score = Array.Empty<ProducerScore>();

        /// <summary>
        /// Observer instance.
        /// </summary>
        public EventEmitter Observer { get; } = new EventEmitter();

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
        /// <param name="loggerFactory"></param>
        /// <param name="internal"></param>
        /// <param name="data"></param>
        /// <param name="channel"></param>
        /// <param name="appData"></param>
        /// <param name="paused"></param>
        public Producer(ILoggerFactory loggerFactory,
            ProducerInternal @internal,
            ProducerData data,
            IChannel channel,
            IPayloadChannel payloadChannel,
            Dictionary<string, object>? appData,
            bool paused
        )
        {
            logger = loggerFactory.CreateLogger<Producer>();

            this.@internal = @internal;
            Data = data;
            this.channel = channel;
            this.payloadChannel = payloadChannel;
            AppData = appData ?? new Dictionary<string, object>();
            Paused = paused;
            pauseLock.Set();

            if (isCheckConsumer)
            {
                checkConsumersTimer = new Timer(CheckConsumers, null, TimeSpan.FromSeconds(CheckConsumersTimeSeconds),
                    TimeSpan.FromMilliseconds(-1));
            }

            HandleWorkerNotifications();
        }

        /// <summary>
        /// Close the Producer.
        /// </summary>
        public async Task CloseAsync()
        {
            logger.LogDebug($"CloseAsync() | Producer:{ProducerId}");

            using (await closeLock.WriteLockAsync())
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
            channel.MessageEvent -= OnChannelMessage;
            //_payloadChannel.MessageEvent -= OnPayloadChannelMessage;

            var reqData = new { ProducerId = @internal.ProducerId };

            // Fire and forget
            channel.RequestAsync(MethodId.TRANSPORT_CLOSE_PRODUCER, @internal.RouterId, reqData)
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
            logger.LogDebug($"TransportClosedAsync() | Producer:{ProducerId}");

            using (await closeLock.WriteLockAsync())
            {
                if (Closed)
                {
                    return;
                }

                Closed = true;

                if (checkConsumersTimer != null)
                {
                    await checkConsumersTimer.DisposeAsync();
                }

                // Remove notification subscriptions.
                channel.MessageEvent -= OnChannelMessage;
                //_payloadChannel.MessageEvent -= OnPayloadChannelMessage;

                Emit("transportclose");

                // Emit observer event.
                Observer.Emit("close");
            }
        }

        /// <summary>
        /// Dump DataProducer.
        /// </summary>
        public async Task<string> DumpAsync()
        {
            logger.LogDebug($"DumpAsync() | Producer:{ProducerId}");

            using (await closeLock.ReadLockAsync())
            {
                if (Closed)
                {
                    throw new InvalidStateException("Producer closed");
                }

                return (await channel.RequestAsync(MethodId.PRODUCER_DUMP, @internal.ProducerId))!;
            }
        }

        /// <summary>
        /// Get DataProducer stats.
        /// </summary>
        public async Task<string> GetStatsAsync()
        {
            logger.LogDebug($"GetStatsAsync() | Producer:{ProducerId}");

            using (await closeLock.ReadLockAsync())
            {
                if (Closed)
                {
                    throw new InvalidStateException("Producer closed");
                }

                return (await channel.RequestAsync(MethodId.PRODUCER_GET_STATS, @internal.ProducerId))!;
            }
        }

        /// <summary>
        /// Pause the Producer.
        /// </summary>
        public async Task PauseAsync()
        {
            logger.LogDebug($"PauseAsync() | Producer:{ProducerId}");

            using (await closeLock.ReadLockAsync())
            {
                if (Closed)
                {
                    throw new InvalidStateException("Producer closed");
                }

                await pauseLock.WaitAsync();
                try
                {
                    var wasPaused = Paused;

                    await channel.RequestAsync(MethodId.PRODUCER_PAUSE, @internal.ProducerId);

                    Paused = true;

                    // Emit observer event.
                    if (!wasPaused)
                    {
                        Observer.Emit("pause");
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
            logger.LogDebug($"ResumeAsync() | Producer:{ProducerId}");

            using (await closeLock.ReadLockAsync())
            {
                if (Closed)
                {
                    throw new InvalidStateException("Producer closed");
                }

                await pauseLock.WaitAsync();
                try
                {
                    var wasPaused = Paused;

                    await channel.RequestAsync(MethodId.PRODUCER_RESUME, @internal.ProducerId);

                    Paused = false;

                    // Emit observer event.
                    if (wasPaused)
                    {
                        Observer.Emit("resume");
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
        public async Task EnableTraceEventAsync(TraceEventType[] types)
        {
            logger.LogDebug($"EnableTraceEventAsync() | Producer:{ProducerId}");

            using (await closeLock.ReadLockAsync())
            {
                if (Closed)
                {
                    throw new InvalidStateException("Producer closed");
                }

                var reqData = new
                {
                    Types = types ?? Array.Empty<TraceEventType>()
                };

                await channel.RequestAsync(MethodId.PRODUCER_ENABLE_TRACE_EVENT, @internal.ProducerId, reqData);
            }
        }

        /// <summary>
        /// Send RTP packet (just valid for Producers created on a DirectTransport).
        /// </summary>
        /// <param name="rtpPacket"></param>
        public async Task SendAsync(byte[] rtpPacket)
        {
            using (await closeLock.ReadLockAsync())
            {
                if (Closed)
                {
                    throw new InvalidStateException("Producer closed");
                }

                await payloadChannel.NotifyAsync("producer.send", @internal.ProducerId, null, rtpPacket);
            }
        }

        public async Task AddConsumerAsync(Consumer.Consumer consumer)
        {
            using (await closeLock.ReadLockAsync())
            {
                if (Closed)
                {
                    throw new InvalidStateException("Producer closed");
                }

                consumers[consumer.ConsumerId] = consumer;
            }
        }

        public async Task RemoveConsumerAsync(string consumerId)
        {
            logger.LogDebug($"RemoveConsumer() | Producer:{ProducerId} ConsumerId:{consumerId}");

            using (await closeLock.ReadLockAsync())
            {
                // 关闭后也允许移除
                consumers.Remove(consumerId);
            }
        }

        #region Event Handlers

        private void HandleWorkerNotifications()
        {
            channel.MessageEvent += OnChannelMessage;
        }

        private void OnChannelMessage(string targetId, string @event, string? data)
        {
            if (targetId != ProducerId)
            {
                return;
            }

            switch (@event)
            {
                case "score":
                {
                    var score = data!.Deserialize<ProducerScore[]>();
                    Score = score;

                    Emit("score", score);

                    // Emit observer event.
                    Observer.Emit("score", score);

                    break;
                }
                case "videoorientationchange":
                {
                    var videoOrientation = data!.Deserialize<ProducerVideoOrientation>();

                    Emit("videoorientationchange", videoOrientation);

                    // Emit observer event.
                    Observer.Emit("videoorientationchange", videoOrientation);

                    break;
                }
                case "trace":
                {
                    var trace = data!.Deserialize<TransportTraceEventData>();

                    Emit("trace", trace);

                    // Emit observer event.
                    Observer.Emit("trace", trace);

                    break;
                }
                default:
                {
                    logger.LogError($"OnChannelMessage() | Ignoring unknown event{@event}");
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
            logger.LogDebug($"CheckConsumer() | Producer: {@internal.ProducerId} Consumers: {consumers.Count}");

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
                    checkConsumersTimer?.Change(TimeSpan.FromSeconds(CheckConsumersTimeSeconds),
                        TimeSpan.FromMilliseconds(-1));
                }
            }
        }

        #endregion Private Methods
    }
}