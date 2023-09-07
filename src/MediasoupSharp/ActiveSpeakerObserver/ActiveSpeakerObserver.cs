using MediasoupSharp.RtpObserver;
using Microsoft.Extensions.Logging;

namespace MediasoupSharp.ActiveSpeakerObserver
{
    public class ActiveSpeakerObserver<TActiveSpeakerObserverAppData> :
        RtpObserver<TActiveSpeakerObserverAppData, ActiveSpeakerObserverEvents>
    {
        /// <summary>
        /// Logger.
        /// </summary>
        private readonly ILogger logger;

        public ActiveSpeakerObserver(ILoggerFactory loggerFactory,
            RtpObserverObserverConstructorOptions<TActiveSpeakerObserverAppData> args
        ) : base(loggerFactory, args)
        {
            logger = loggerFactory.CreateLogger(GetType());
            HandleWorkerNotifications();
        }

        public IEnhancedEventEmitter<ActiveSpeakerObserverObserverEvents> Observer =>
            base.Observer as IEnhancedEventEmitter<ActiveSpeakerObserverObserverEvents>;


        private void HandleWorkerNotifications()
        {
            Channel.On(Internal.RtpObserverId, async args =>
            {
                var @event = args![0] as string;
                var data = args.Length > 0 ? (dynamic)args[1] : null;
                switch (@event)
                {
                    case "dominantspeaker":
                    {
                        var producer = this.GetProducerById(data!.producerId);

                        if (!producer)
                        {
                            break;
                        }

                        ActiveSpeakerObserverDominantSpeaker dominantSpeaker = new(producer);

                        await SafeEmit("dominantspeaker", dominantSpeaker);
                        await Observer.SafeEmit("dominantspeaker", dominantSpeaker);
                        break;
                    }

                    default:
                    {
                        logger.LogError("ignoring unknown event '{E}' ", @event);
                        break;
                    }
                }
            });
        }
    }
}