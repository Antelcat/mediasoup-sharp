using Antelcat.AutoGen.ComponentModel.Diagnostic;
using Antelcat.MediasoupSharp.FBS.Notification;
using Microsoft.Extensions.Logging;

namespace Antelcat.MediasoupSharp;

[AutoExtractInterface(NamingTemplate = nameof(IAudioLevelObserver))]
public class AudioLevelObserver<TAudioLevelObserverAppData>
    : RtpObserverImpl<
            TAudioLevelObserverAppData, 
            AudioLevelObserverEvents, 
            AudioLevelObserverObserver
        >, IAudioLevelObserver<TAudioLevelObserverAppData>
    where TAudioLevelObserverAppData : new()
{
    /// <summary>
    /// Logger.
    /// </summary>
    private readonly ILogger logger = new Logger<IAudioLevelObserver>();

    /// <summary>
    /// <para>Events:</para>
    /// <para>@emits <see cref="AudioLevelObserverEvents.Volumes"/></para>
    /// <para>@emits <see cref="AudioLevelObserverEvents.Silence"/></para>
    /// <para>Observer events:</para>
    /// <para>@emits <see cref="AudioLevelObserverObserverEvents.Volumes"/></para>
    /// <para>@emits <see cref="AudioLevelObserverObserverEvents.Silence"/></para>
    /// </summary>
    public AudioLevelObserver(AudioLevelObserverConstructorOptions<TAudioLevelObserverAppData> options) : base(options,
        new())
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
                        volumes.Add(new AudioLevelObserverVolume { Producer = producer, Volume = item.Volume_ });
                    }
                }

                if (volumes.Count > 0)
                {
                    this.Emit(static x => x.Volumes, volumes);

                    // Emit observer event.
                    Observer.Emit(static x => x.Volumes, volumes);
                }

                break;
            }
            case Event.AUDIOLEVELOBSERVER_SILENCE:
            {
                this.Emit(static x => x.Silence);

                // Emit observer event.
                Observer.Emit(static x => x.Silence);

                break;
            }
            default:
            {
                logger.LogError($"{nameof(OnNotificationHandle)}() | Ignoring unknown event: {{Event}}", @event);
                break;
            }
        }
    }
}