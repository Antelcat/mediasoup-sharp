using Microsoft.Extensions.Logging;

namespace MediasoupSharp.RtpObserver
{
    public abstract class RtpObserver<TRtpObserverAppData, TEvents> : EnhancedEventEmitter<TEvents> 
        where TEvents : RtpObserverEvents
    {
        /// <summary>
        /// Logger.
        /// </summary>
        private readonly ILogger logger;

        /// <summary>
        /// Whether the Producer is closed.
        /// </summary>
        private bool closed;

        /// <summary>
        /// Paused flag.
        /// </summary>
        private bool paused;

        /// <summary>
        /// Internal data.
        /// </summary>
        public RtpObserverInternal Internal { get; }

        /// <summary>
        /// Channel instance.
        /// </summary>
        protected readonly Channel.Channel Channel;

        /// <summary>
        /// PayloadChannel instance.
        /// </summary>
        protected readonly PayloadChannel.PayloadChannel PayloadChannel;

        /// <summary>
        /// App custom data.
        /// </summary>
        public TRtpObserverAppData? AppData { get; set; }

        /// <summary>
        /// Method to retrieve a Producer.
        /// </summary>
        protected readonly Func<string, Task<Producer.Producer?>> GetProducerById;

        /// <summary>
        /// Observer instance.
        /// </summary>
        public EnhancedEventEmitter<RtpObserverObserverEvents> Observer { get; }

        /// <summary>
        /// <para>Events:</para>
        /// <para>@emits routerclose</para>
        /// <para>@emits @close</para>
        /// <para>Observer events:</para>
        /// <para>@emits close</para>
        /// <para>@emits pause</para>
        /// <para>@emits resume</para>
        /// <para>@emits addproducer - (producer: Producer)</para>
        /// <para>@emits removeproducer - (producer: Producer)</para>
        /// </summary>
        /// <param name="loggerFactory"></param>
        /// <param name="internal"></param>
        /// <param name="channel"></param>
        /// <param name="payloadChannel"></param>
        /// <param name="appData"></param>
        /// <param name="getProducerById"></param>
        protected RtpObserver(ILoggerFactory loggerFactory,
            RtpObserverInternal @internal,
            Channel.Channel channel,
            PayloadChannel.PayloadChannel payloadChannel,
            TRtpObserverAppData? appData,
            Func<string, Task<Producer.Producer?>> getProducerById
        ) : base(loggerFactory.CreateLogger("EnhancedEventEmitter"))
        {
            logger = loggerFactory.CreateLogger(typeof(RtpObserver<,>));

            Internal = @internal;
            Channel = channel;
            PayloadChannel = payloadChannel;
            AppData = appData ?? default;
            GetProducerById = getProducerById;
            Observer = new EnhancedEventEmitter<RtpObserverObserverEvents>(logger);
        }

        /// <summary>
        /// Close the RtpObserver.
        /// </summary>
        public void Close()
        {
            if (closed)
            {
                return;
            }

            logger.LogDebug("Close() | RtpObserver:{RtpObserverId}", Internal.RtpObserverId);

            closed = true;

            // Remove notification subscriptions.
            Channel.MessageEvent -= OnChannelMessage;
            //PayloadChannel.MessageEvent -= OnPayloadChannelMessage;

            var reqData = new { RtpObserverId = Internal.RtpObserverId };

            // Fire and forget
            Channel.RequestAsync("router.closeRtpObserver", Internal.RouterId, reqData)
                .ContinueWith((t) => { }, TaskContinuationOptions.OnlyOnFaulted);

            _ = Emit("@close");

            // Emit observer event.
            _ = Observer.SafeEmit("close");
        }

        public bool Closed => closed;
        public bool Paused => paused;


        /// <summary>
        /// Router was closed.
        /// </summary>
        public async Task RouterClosedAsync()
        {
            if (closed)
            {
                return;
            }

            logger.LogDebug("RouterClosed() | RtpObserver:{InternalRtpObserverId}", Internal.RtpObserverId);

            closed = true;

            // Remove notification subscriptions.
            Channel.MessageEvent -= OnChannelMessage;
            //PayloadChannel.MessageEvent -= OnPayloadChannelMessage;

            _ = SafeEmit("routerclose");

            // Emit observer event.
            _ = Observer.SafeEmit("close");
        }

        /// <summary>
        /// Pause the RtpObserver.
        /// </summary>
        public async Task PauseAsync()
        {
            logger.LogDebug("PauseAsync() | RtpObserver:{InternalRtpObserverId}", Internal.RtpObserverId);

            var wasPaused = paused;

            // Fire and forget
            await Channel.RequestAsync("rtpObserver.pause", Internal.RtpObserverId);

            paused = true;

            // Emit observer event.
            if (!wasPaused)
            {
                await Observer.SafeEmit("pause");
            }
        }

        /// <summary>
        /// Resume the RtpObserver.
        /// </summary>
        public async Task ResumeAsync()
        {
            logger.LogDebug("ResumeAsync() | RtpObserver:{InternalRtpObserverId}", Internal.RtpObserverId);

            var wasPaused = paused;

            await Channel.RequestAsync("rtpObserver.resume", Internal.RtpObserverId);

            paused = false;

            // Emit observer event.
            if (wasPaused)
            {
                await Observer.Emit("resume");
            }
        }

        /// <summary>
        /// Add a Producer to the RtpObserver.
        /// </summary>
        public async Task AddProducerAsync(RtpObserverAddRemoveProducerOptions rtpObserverAddRemoveProducerOptions)
        {
            var producerId = rtpObserverAddRemoveProducerOptions.ProducerId;
            logger.LogDebug("AddProducerAsync() | RtpObserver:{InternalRtpObserverId}", producerId);

            var producer = await GetProducerById(producerId);

            if (producer == null)
            {
                throw new KeyNotFoundException($"Producer with id {producerId} not found");
            }

            var reqData = new { producerId };
            // Fire and forget
            await Channel.RequestAsync("rtpObserver.addProducer", Internal.RtpObserverId, reqData);

            // Emit observer event.
            await Observer.Emit("addproducer", producer);
        }

        /// <summary>
        /// Remove a Producer from the RtpObserver.
        /// </summary>
        public async Task RemoveProducerAsync(RtpObserverAddRemoveProducerOptions rtpObserverAddRemoveProducerOptions)
        {
            var producerId = rtpObserverAddRemoveProducerOptions.ProducerId;
            logger.LogDebug("RemoveProducerAsync() | RtpObserver:{InternalRtpObserverId}", Internal.RtpObserverId);

            var producer = await GetProducerById(producerId);
            if (producer == null)
            {
                throw new KeyNotFoundException($"Producer with id {producerId} not found");
            }

            var reqData = new { producerId };
            // Fire and forget
            await Channel.RequestAsync("rtpObserver.removeProducer", Internal.RtpObserverId, reqData);

            // Emit observer event.
            await Observer.Emit("removeproducer", producer);
        }

        #region Event Handlers

        protected abstract void OnChannelMessage(string targetId, string @event, string? data);

        #endregion Event Handlers
    }
}