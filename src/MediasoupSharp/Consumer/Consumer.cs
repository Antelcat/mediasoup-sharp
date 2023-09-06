using MediasoupSharp.Channel;
using MediasoupSharp.Exceptions;
using MediasoupSharp.PayloadChannel;

namespace MediasoupSharp.Consumer
{
    public class Consumer : EventEmitter
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
        public string ConsumerId => @internal.ConsumerId;

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
        /// PayloadChannel instance.
        /// </summary>
        private readonly IPayloadChannel payloadChannel;

        /// <summary>
        /// App custom data.
        /// </summary>
        public Dictionary<string, object> AppData { get; }

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
        public ConsumerScore? Score;

        /// <summary>
        /// Preferred layers.
        /// </summary>
        public ConsumerLayers? PreferredLayers { get; private set; }

        /// <summary>
        /// Curent layers.
        /// </summary>
        public ConsumerLayers? CurrentLayers { get; private set; }

        /// <summary>
        /// Observer instance.
        /// </summary>
        public EventEmitter Observer { get; } = new EventEmitter();

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
        /// <param name="loggerFactory"></param>
        /// <param name="internal"></param>
        /// <param name="data"></param>
        /// <param name="channel"></param>
        /// <param name="appData"></param>
        /// <param name="paused"></param>
        /// <param name="producerPaused"></param>
        /// <param name="score"></param>
        /// <param name="preferredLayers"></param>
        public Consumer(ILoggerFactory loggerFactory,
            ConsumerInternal @internal,
            ConsumerData data,
            IChannel channel,
            IPayloadChannel payloadChannel,
            Dictionary<string, object>? appData,
            bool paused,
            bool producerPaused,
            ConsumerScore? score,
            ConsumerLayers? preferredLayers
            )
        {
            logger = loggerFactory.CreateLogger<Consumer>();

            this.@internal = @internal;
            Data = data;
            this.channel = channel;
            this.payloadChannel = payloadChannel;
            AppData = appData ?? new Dictionary<string, object>();
            this.paused = paused;
            ProducerPaused = producerPaused;
            Score = score;
            PreferredLayers = preferredLayers;
            pauseLock.Set();

            HandleWorkerNotifications();
        }

        /// <summary>
        /// Close the Producer.
        /// </summary>
        public async Task CloseAsync()
        {
            logger.LogDebug($"CloseAsync() | Consumer:{ConsumerId}");

            using (await closeLock.WriteLockAsync())
            {
                if (closed)
                {
                    return;
                }

                closed = true;

                // Remove notification subscriptions.
                channel.MessageEvent -= OnChannelMessage;
                payloadChannel.MessageEvent -= OnPayloadChannelMessage;

                var reqData = new { ConsumerId = @internal.ConsumerId };

                // Fire and forget
                channel.RequestAsync(MethodId.TRANSPORT_CLOSE_CONSUMER, @internal.TransportId, reqData).ContinueWithOnFaultedHandleLog(logger);

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
            logger.LogDebug($"TransportClosed() | Consumer:{ConsumerId}");

            using (await closeLock.WriteLockAsync())
            {
                if (closed)
                {
                    return;
                }

                closed = true;

                // Remove notification subscriptions.
                channel.MessageEvent -= OnChannelMessage;
                payloadChannel.MessageEvent -= OnPayloadChannelMessage;

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
            logger.LogDebug($"DumpAsync() | Consumer:{ConsumerId}");

            using (await closeLock.ReadLockAsync())
            {
                if(closed)
                {
                    throw new InvalidStateException("Consumer closed");
                }

                return (await channel.RequestAsync(MethodId.CONSUMER_DUMP, @internal.ConsumerId))!;
            }
        }

        /// <summary>
        /// Get DataProducer stats.
        /// </summary>
        public async Task<string> GetStatsAsync()
        {
            logger.LogDebug($"GetStatsAsync() | Consumer:{ConsumerId}");

            using (await closeLock.ReadLockAsync())
            {
                if (closed)
                {
                    throw new InvalidStateException("Consumer closed");
                }

                return (await channel.RequestAsync(MethodId.CONSUMER_GET_STATS, @internal.ConsumerId))!;
            }
        }

        /// <summary>
        /// Pause the Consumer.
        /// </summary>
        public async Task PauseAsync()
        {
            logger.LogDebug($"PauseAsync() | Consumer:{ConsumerId}");

            using (await closeLock.ReadLockAsync())
            {
                if (closed)
                {
                    throw new InvalidStateException("Consumer closed");
                }

                await pauseLock.WaitAsync();
                try
                {
                    var wasPaused = paused || ProducerPaused;

                    // Fire and forget
                    channel.RequestAsync(MethodId.CONSUMER_PAUSE, @internal.ConsumerId).ContinueWithOnFaultedHandleLog(logger);

                    paused = true;

                    // Emit observer event.
                    if (!wasPaused)
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
            logger.LogDebug($"ResumeAsync() | Consumer:{ConsumerId}");

            using (await closeLock.ReadLockAsync())
            {
                if (closed)
                {
                    throw new InvalidStateException("Consumer closed");
                }

                await pauseLock.WaitAsync();
                try
                {
                    var wasPaused = paused || ProducerPaused;

                    // Fire and forget
                    channel.RequestAsync(MethodId.CONSUMER_RESUME, @internal.ConsumerId).ContinueWithOnFaultedHandleLog(logger);

                    paused = false;

                    // Emit observer event.
                    if (wasPaused && !ProducerPaused)
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
        /// Set preferred video layers.
        /// </summary>
        public async Task SetPreferredLayersAsync(ConsumerLayers consumerLayers)
        {
            logger.LogDebug($"SetPreferredLayersAsync() | Consumer:{ConsumerId}");

            using (await closeLock.ReadLockAsync())
            {
                if (closed)
                {
                    throw new InvalidStateException("Consumer closed");
                }

                var reqData = consumerLayers;
                var resData = await channel.RequestAsync(MethodId.CONSUMER_SET_PREFERRED_LAYERS, @internal.ConsumerId, reqData);
                var responseData = resData!.Deserialize<ConsumerSetPreferredLayersResponseData>()!;
                PreferredLayers = responseData;
            }
        }

        /// <summary>
        /// Set priority.
        /// </summary>
        public async Task SetPriorityAsync(int priority)
        {
            logger.LogDebug($"SetPriorityAsync() | Consumer:{ConsumerId}");

            using (await closeLock.ReadLockAsync())
            {
                if (closed)
                {
                    throw new InvalidStateException("Consumer closed");
                }

                var reqData = new { Priority = priority };
                var resData = await channel.RequestAsync(MethodId.CONSUMER_SET_PRIORITY, @internal.ConsumerId, reqData);
                var responseData = resData!.Deserialize<ConsumerSetOrUnsetPriorityResponseData>();
                Priority = responseData!.Priority;
            }
        }

        /// <summary>
        /// Unset priority.
        /// </summary>
        public async Task UnsetPriorityAsync()
        {
            logger.LogDebug($"UnsetPriorityAsync() | Consumer:{ConsumerId}");

            using (await closeLock.ReadLockAsync())
            {
                if (closed)
                {
                    throw new InvalidStateException("Consumer closed");
                }

                var reqData = new { Priority = 1 };
                var resData = await channel.RequestAsync(MethodId.CONSUMER_SET_PRIORITY, @internal.ConsumerId, reqData);
                var responseData = resData!.Deserialize<ConsumerSetOrUnsetPriorityResponseData>();
                Priority = responseData!.Priority;
            }
        }

        /// <summary>
        /// Request a key frame to the Producer.
        /// </summary>
        public async Task RequestKeyFrameAsync()
        {
            logger.LogDebug($"RequestKeyFrameAsync() | Consumer:{ConsumerId}");

            using (await closeLock.ReadLockAsync())
            {
                if (closed)
                {
                    throw new InvalidStateException("Consumer closed");
                }

                await channel.RequestAsync(MethodId.CONSUMER_REQUEST_KEY_FRAME, @internal.ConsumerId);
            }
        }

        /// <summary>
        /// Enable 'trace' event.
        /// </summary>
        public async Task EnableTraceEventAsync(TraceEventType[] types)
        {
            logger.LogDebug($"EnableTraceEventAsync() | Consumer:{ConsumerId}");

            var reqData = new
            {
                Types = types ?? Array.Empty<TraceEventType>()
            };

            using (await closeLock.ReadLockAsync())
            {
                if (closed)
                {
                    throw new InvalidStateException("Consumer closed");
                }

                // Fire and forget
                channel.RequestAsync(MethodId.CONSUMER_ENABLE_TRACE_EVENT, @internal.ConsumerId, reqData).ContinueWithOnFaultedHandleLog(logger);
            }
        }

        #region Event Handlers

        private void HandleWorkerNotifications()
        {
            channel.MessageEvent += OnChannelMessage;
            payloadChannel.MessageEvent += OnPayloadChannelMessage;
        }

#pragma warning disable VSTHRD100 // Avoid async void methods
        private async void OnChannelMessage(string targetId, string @event, string? data)
#pragma warning restore VSTHRD100 // Avoid async void methods
        {
            if (targetId != ConsumerId)
            {
                return;
            }

            switch (@event)
            {
                case "producerclose":
                    {
                        using (await closeLock.WriteLockAsync())
                        {
                            if (closed)
                            {
                                break;
                            }

                            closed = true;

                            // Remove notification subscriptions.
                            channel.MessageEvent -= OnChannelMessage;
                            payloadChannel.MessageEvent -= OnPayloadChannelMessage;

                            Emit("@producerclose");
                            Emit("producerclose");

                            // Emit observer event.
                            Observer.Emit("close");
                        }

                        break;
                    }
                case "producerpause":
                    {
                        if (ProducerPaused)
                        {
                            break;
                        }

                        var wasPaused = paused || ProducerPaused;

                        ProducerPaused = true;

                        Emit("producerpause");

                        // Emit observer event.
                        if (!wasPaused)
                        {
                            Observer.Emit("pause");
                        }

                        break;
                    }
                case "producerresume":
                    {
                        if (!ProducerPaused)
                        {
                            break;
                        }

                        var wasPaused = paused || ProducerPaused;

                        ProducerPaused = false;

                        Emit("producerresume");

                        // Emit observer event.
                        if (wasPaused && !paused)
                        {
                            Observer.Emit("resume");
                        }

                        break;
                    }
                case "score":
                    {
                        var score = data!.Deserialize<ConsumerScore>();
                        Score = score;

                        Emit("score", score);

                        // Emit observer event.
                        Observer.Emit("score", score);

                        break;
                    }
                case "layerschange":
                    {
                        var layers = !data.IsNullOrWhiteSpace() ? data?.Deserialize<ConsumerLayers>() : null;

                        CurrentLayers = layers;

                        Emit("layerschange", layers);

                        // Emit observer event.
                        Observer.Emit("layersChange", layers);

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

        private void OnPayloadChannelMessage(string targetId, string @event, string? data, ArraySegment<byte> payload)
        {
            if (targetId != ConsumerId)
            {
                return;
            }

            switch (@event)
            {
                case "rtp":
                    {
                        Emit("rtp", payload);

                        break;
                    }
                default:
                    {
                        logger.LogError($"OnPayloadChannelMessage() | Ignoring unknown event \"{@event}\"");
                        break;
                    }
            }
        }
    }

    #endregion Event Handlers
}
