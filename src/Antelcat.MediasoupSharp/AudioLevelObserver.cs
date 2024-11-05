using Antelcat.AutoGen.ComponentModel.Diagnostic;
using FBS.Notification;
using Microsoft.Extensions.Logging;

namespace Antelcat.MediasoupSharp;

using AudioLevelObserverObserver = EnhancedEventEmitter<AudioLevelObserverObserverEvents>;

public class AudioLevelObserverOptions<TAudioLevelObserverAppData>
{
    /// <summary>
    /// Maximum number of entries in the 'volumes”' event. Default 1.
    /// </summary>
    public ushort MaxEntries { get; set; } = 1;

    /// <summary>
    /// Minimum average volume (in dBvo from -127 to 0) for entries in the
    /// 'volumes' event. Default -80.
    /// </summary>
    public sbyte Threshold { get; set; } = -80;

    /// <summary>
    /// Interval in ms for checking audio volumes. Default 1000.
    /// </summary>
    public ushort Interval { get; set; } = 1000;

    /// <summary>
    /// Custom application data.
    /// </summary>
    public TAudioLevelObserverAppData? AppData { get; set; }
}

public class AudioLevelObserverVolume
{
    /// <summary>
    /// The audio Producer instance.
    /// </summary>
    public IProducer Producer { get; set; }

    /// <summary>
    /// The average volume (in dBvo from -127 to 0) of the audio Producer in the
    /// last interval.
    /// </summary>
    public int Volume { get; set; }
}

public class AudioLevelObserverEvents : RtpObserverEvents
{
    public AudioLevelObserverVolume[] volumes;
    public object?                    silence;
}


public class AudioLevelObserverObserverEvents : RtpObserverObserverEvents
{
    public AudioLevelObserverVolume[] volumes;
    public object?                    silence;
}

public class AudioLevelObserverConstructorOptions<TAudioLevelObserverAppData>
    : RtpObserverConstructorOptions<TAudioLevelObserverAppData>;

[AutoExtractInterface]
public class AudioLevelObserver<TAudioLevelObserverAppData> 
    : RtpObserver<TAudioLevelObserverAppData, AudioLevelObserverEvents, AudioLevelObserverObserver> , IAudioLevelObserver
where TAudioLevelObserverAppData : new()
{
    /// <summary>
    /// Logger.
    /// </summary>
    private readonly ILogger logger = new Logger<AudioLevelObserver<TAudioLevelObserverAppData>>();

    /// <summary>
    /// <para>Events:</para>
    /// <para>@emits volumes - (volumes: AudioLevelObserverVolume[])</para>
    /// <para>@emits silence</para>
    /// <para>Observer events:</para>
    /// <para>@emits close</para>
    /// <para>@emits pause</para>
    /// <para>@emits resume</para>
    /// <para>@emits addproducer - (producer: Producer)</para>
    /// <para>@emits removeproducer - (producer: Producer)</para>
    /// <para>@emits volumes - (volumes: AudioLevelObserverVolume[])</para>
    /// <para>@emits silence</para>
    /// </summary>
    public AudioLevelObserver(
        AudioLevelObserverConstructorOptions<TAudioLevelObserverAppData> options
    )
        : base(options, new())
    {
    }

#pragma warning disable VSTHRD100 // Avoid async void methods
    protected override async void OnNotificationHandle(string handlerId, Event @event, Notification data)
#pragma warning restore VSTHRD100 // Avoid async void methods
    {
        if (handlerId != Internal.RtpObserverId)
        {
            return;
        }

        switch (@event)
        {
            case Event.AUDIOLEVELOBSERVER_VOLUMES:
            {
                var volumesNotification = data.BodyAsAudioLevelObserver_VolumesNotification().UnPack();

                var volumes = new List<AudioLevelObserverVolume>();
                foreach (var item in volumesNotification.Volumes)
                {
                    var producer = await GetProducerById(item.ProducerId);
                    if (producer != null)
                    {
                        volumes.Add(new AudioLevelObserverVolume { Producer = producer, Volume = item.Volume_, });
                    }
                }

                if (volumes.Count > 0)
                {
                    Emit(nameof(volumes), volumes);

                    // Emit observer event.
                    Observer.Emit(nameof(volumes), volumes);
                }

                break;
            }
            case Event.AUDIOLEVELOBSERVER_SILENCE:
            {
                Emit("silence");

                // Emit observer event.
                Observer.Emit("silence");

                break;
            }
            default:
            {
                logger.LogError("OnNotificationHandle() | Ignoring unknown event: {@event}", @event);
                break;
            }
        }
    }
}