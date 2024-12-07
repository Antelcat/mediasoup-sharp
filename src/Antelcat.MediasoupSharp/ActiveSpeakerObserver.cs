using Antelcat.AutoGen.ComponentModel.Diagnostic;
using Antelcat.MediasoupSharp.FBS.Notification;
using Microsoft.Extensions.Logging;

namespace Antelcat.MediasoupSharp;

[AutoExtractInterface(NamingTemplate = nameof(IActiveSpeakerObserver))]
public class ActiveSpeakerObserverImpl<TActiveSpeakerObserverAppData>
    : RtpObserverImpl<
            TActiveSpeakerObserverAppData,
            ActiveSpeakerObserverEvents,
            ActiveSpeakerObserverObserver
        >, IActiveSpeakerObserver<TActiveSpeakerObserverAppData>
    where TActiveSpeakerObserverAppData : new()
{
    /// <summary>
    /// Logger.
    /// </summary>
    private readonly ILogger logger = new Logger<IAudioLevelObserver>();

    /// <summary>
    /// <para>Events:</para>
    /// <para>@emits <see cref="ActiveSpeakerObserverEvents.DominantSpeaker"/></para>
    /// <para>Observer events:</para>
    /// <para>@emits <see cref="ActiveSpeakerObserverObserverEvents.DominantSpeaker"/></para>
    /// </summary>
    public ActiveSpeakerObserverImpl(RtpObserverObserverConstructorOptions<TActiveSpeakerObserverAppData> options)
        : base(options, new())
    {
        HandleListenerError();
    }

#pragma warning disable VSTHRD100
    protected override async void OnNotificationHandle(string handlerId, Event @event, Notification notification)
#pragma warning restore VSTHRD100
    {
        if (handlerId != Internal.RtpObserverId)
        {
            return;
        }

        switch (@event)
        {
            case Event.ACTIVESPEAKEROBSERVER_DOMINANT_SPEAKER:
            {
                var dominantSpeakerNotification =
                    notification.BodyAsActiveSpeakerObserver_DominantSpeakerNotification().UnPack();

                var producer = await GetProducerById(dominantSpeakerNotification.ProducerId);
                if (producer != null)
                {
                    var dominantSpeaker = new ActiveSpeakerObserverDominantSpeaker
                    {
                        Producer = await GetProducerById(dominantSpeakerNotification.ProducerId)
                    };

                    this.SafeEmit(static x => x.DominantSpeaker, dominantSpeaker);

                    // Emit observer event.
                    Observer.SafeEmit(static x => x.DominantSpeaker, dominantSpeaker);
                }

                break;
            }
            default:
            {
                logger.LogError($"{nameof(OnNotificationHandle)}() | Ignoring unknown event: {{Event}}", @event);
                break;
            }
        }
    }

    protected void HandleListenerError() =>
        this.On(static x=>x.ListenerError, tuple =>
        {
            logger.LogError(tuple.error,
                "event listener threw an error [eventName:{EventName}]:",
                tuple.eventName);
        });
}