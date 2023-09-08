using MediasoupSharp.RtpObserver;
using Microsoft.Extensions.Logging;

namespace MediasoupSharp.AudioLevelObserver;

public class AudioLevelObserver<TAudioLevelObserverAppData>
    : RtpObserver<TAudioLevelObserverAppData, AudioLevelObserverEvents>
{
    /// <summary>
    /// Logger.
    /// </summary>
    private readonly ILogger logger;

    public AudioLevelObserver(ILoggerFactory loggerFactory,
        AudioLevelObserverConstructorOptions<TAudioLevelObserverAppData> args
    ) : base(loggerFactory, args)
    {
        logger = loggerFactory.CreateLogger(GetType());
    }

    public IEnhancedEventEmitter<AudioLevelObserverObserverEvents> Observer =>
        base.Observer as IEnhancedEventEmitter<AudioLevelObserverObserverEvents>;

    private void HandleWorkerNotifications()
    {
        Channel.On(Internal.RtpObserverId, async args =>
        {
            var @event = args![0] as string;
            var data = args.Length > 0 ? (dynamic)args[1] : null;
            switch (@event)
            {
                case "volumes":
                {
                    var volumes = ((List<dynamic>)data!)
                        .Select(x =>
                            new AudioLevelObserverVolume
                            {
                                Producer = GetProducerById(x.producerId),
                                Volume = x.volume
                            }
                        ).DistinctBy(x => x.Producer);

                    if (volumes.Any())
                    {
                        _ = Emit(nameof(volumes), volumes);

                        // Emit observer event.
                        _ = Observer.SafeEmit(nameof(volumes), volumes);
                    }

                    break;
                }
                case "silence":
                {
                    _ = SafeEmit("silence");

                    // Emit observer event.
                    _ = Observer.SafeEmit("silence");

                    break;
                }
                default:
                {
                    logger.LogError("OnChannelMessage() | Ignoring unknown event '{Event}' ", @event);
                    break;
                }
            }
        });
    }
}