using Microsoft.Extensions.Logging;

namespace MediasoupSharp.RtpObserver
{
    public abstract class RtpObserver<TRtpObserverAppData, TEvents> 
        : EnhancedEventEmitter<TEvents> 
        where TEvents : RtpObserverEvents
    {
        /// <summary>
        /// Logger.
        /// </summary>
        private readonly ILogger logger;

        /// <summary>
        /// Internal data.
        /// </summary>
        public RtpObserverObserverInternal Internal { get; }

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
        protected readonly Func<string, Producer.Producer?> GetProducerById;

        /// <summary>
        /// Observer instance.
        /// </summary>
        public EnhancedEventEmitter<RtpObserverObserverEvents> Observer { get; }

        protected RtpObserver(ILoggerFactory loggerFactory,
            RtpObserverConstructorOptions<TRtpObserverAppData> args
        ) : base(loggerFactory.CreateLogger("EnhancedEventEmitter"))
        {
            logger = loggerFactory.CreateLogger(typeof(RtpObserver<,>));

            Internal = args.Internal;
            Channel = args.Channel;
            PayloadChannel = args.PayloadChannel;
            AppData = args.AppData ?? default;
            GetProducerById = args.GetProducerById;
            Observer = new EnhancedEventEmitter<RtpObserverObserverEvents>(logger);
        }

        /// <summary>
        /// Whether the Producer is closed.
        /// </summary>
        public bool Closed { get; private set; }

        /// <summary>
        /// Paused flag.
        /// </summary>
        public bool Paused { get; private set; }

        /// <summary>
        /// Close the RtpObserver.
        /// </summary>
        public void Close()
        {
            if (Closed)
            {
                return;
            }

            logger.LogDebug("Close() | RtpObserver:{RtpObserverId}", Internal.RtpObserverId);

            Closed = true;

            // Remove notification subscriptions.
            Channel.RemoveAllListeners(Internal.RtpObserverId);
            PayloadChannel.RemoveAllListeners(Internal.RtpObserverId);

            var reqData = new { rtpObserverId = Internal.RtpObserverId };

            // Fire and forget
            Channel.Request("router.closeRtpObserver", Internal.RouterId, reqData)
                .ContinueWith((t) => { }, TaskContinuationOptions.OnlyOnFaulted);

            _ = Emit("@close");

            // Emit observer event.
            _ = Observer.SafeEmit("close");
        }

        /// <summary>
        /// Router was closed.
        /// </summary>
        public void RouterClosed()
        {
            if (Closed)
            {
                return;
            }

            logger.LogDebug("RouterClosed() | RtpObserver:{InternalRtpObserverId}", Internal.RtpObserverId);

            Closed = true;

            // Remove notification subscriptions.
            Channel.RemoveAllListeners(Internal.RtpObserverId);
            PayloadChannel.RemoveAllListeners(Internal.RtpObserverId);

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

            var wasPaused = Paused;

            // Fire and forget
            await Channel.Request("rtpObserver.pause", Internal.RtpObserverId);

            Paused = true;

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

            var wasPaused = Paused;

            await Channel.Request("rtpObserver.resume", Internal.RtpObserverId);

            Paused = false;

            // Emit observer event.
            if (wasPaused)
            {
                await Observer.SafeEmit("resume");
            }
        }

        /// <summary>
        /// Add a Producer to the RtpObserver.
        /// </summary>
        public async Task AddProducerAsync(RtpObserverAddRemoveProducerOptions rtpObserverAddRemoveProducerOptions)
        {
            var producerId = rtpObserverAddRemoveProducerOptions.ProducerId;
            
            logger.LogDebug("AddProducerAsync() | RtpObserver:{InternalRtpObserverId}", producerId);

            var producer = GetProducerById(producerId);

            if (producer == null)
            {
                throw new KeyNotFoundException($"Producer with id {producerId} not found");
            }

            var reqData = new { producerId };
            
            await Channel.Request("rtpObserver.addProducer", Internal.RtpObserverId, reqData);

            // Emit observer event.
            await Observer.SafeEmit("addproducer", producer);
        }

        /// <summary>
        /// Remove a Producer from the RtpObserver.
        /// </summary>
        public async Task RemoveProducerAsync(RtpObserverAddRemoveProducerOptions rtpObserverAddRemoveProducerOptions)
        {
            var producerId = rtpObserverAddRemoveProducerOptions.ProducerId;
            
            logger.LogDebug("RemoveProducerAsync() | RtpObserver:{InternalRtpObserverId}", Internal.RtpObserverId);
            
            var producer = GetProducerById(producerId);
            
            if (producer == null)
            {
                throw new KeyNotFoundException($"Producer with id {producerId} not found");
            }

            var reqData = new { producerId };
            
            // Fire and forget
            await Channel.Request("rtpObserver.removeProducer", Internal.RtpObserverId, reqData);

            // Emit observer event.
            await Observer.SafeEmit("removeproducer", producer);
        }
    }
}