using MediasoupSharp.RtpObserver;
using Microsoft.Extensions.Logging;

namespace MediasoupSharp.AudioLevelObserver;

internal class AudioLevelObserver<TAudioLevelObserverAppData>
    : RtpObserver<TAudioLevelObserverAppData, AudioLevelObserverEvents>
{

    public AudioLevelObserver(
        AudioLevelObserverConstructorOptions<TAudioLevelObserverAppData> args
    ) : base(args)
    {
    }

    internal IEnhancedEventEmitter<AudioLevelObserverObserverEvents> Observer =>
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
                        await Emit(nameof(volumes), volumes);

                        // Emit observer event.
                        await Observer.SafeEmit(nameof(volumes), volumes);
                    }

                    break;
                }
                case "silence":
                {
                    await SafeEmit("silence");

                    // Emit observer event.
                    await Observer.SafeEmit("silence");

                    break;
                }
                default:
                {
                    Logger?.LogError("OnChannelMessage() | Ignoring unknown event '{Event}' ", @event);
                    break;
                }
            }
        });
    }
}