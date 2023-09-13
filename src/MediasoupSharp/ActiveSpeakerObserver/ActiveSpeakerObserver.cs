using MediasoupSharp.RtpObserver;
using Microsoft.Extensions.Logging;

namespace MediasoupSharp.ActiveSpeakerObserver;

internal class ActiveSpeakerObserver<TActiveSpeakerObserverAppData> :
    RtpObserver<TActiveSpeakerObserverAppData, ActiveSpeakerObserverEvents>
{
    private readonly ILogger? logger;
    public ActiveSpeakerObserver(
        RtpObserverObserverConstructorOptions<TActiveSpeakerObserverAppData> args,
        ILoggerFactory? loggerFactory = null
    ) : base(args,loggerFactory)
    {
        logger = loggerFactory?.CreateLogger(GetType());
        
        HandleWorkerNotifications();
    }

    internal IEnhancedEventEmitter<ActiveSpeakerObserverObserverEvents> Observer =>
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
                    var producer = GetProducerById(data!.producerId);

                    if (!producer)
                    {
                        break;
                    }

                    ActiveSpeakerObserverDominantSpeaker dominantSpeaker = new()
                    {
                        Producer = producer
                    };

                    await SafeEmit("dominantspeaker", dominantSpeaker);
                    await Observer.SafeEmit("dominantspeaker", dominantSpeaker);
                    break;
                }

                default:
                {
                    logger?.LogError("ignoring unknown event '{E}' ", @event);
                    break;
                }
            }
        });
    }
}