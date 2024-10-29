using Antelcat.MediasoupSharp.Channel;
using Antelcat.MediasoupSharp.EnhancedEvent;
using Antelcat.MediasoupSharp.RtpObserver;
using FBS.Notification;
using Microsoft.Extensions.Logging;

namespace Antelcat.MediasoupSharp.ActiveSpeakerObserver;

public class ActiveSpeakerObserver : RtpObserver.RtpObserver
{
    /// <summary>
    /// Logger.
    /// </summary>
    private readonly ILogger<ActiveSpeakerObserver> logger;

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
    public ActiveSpeakerObserver(
        ILoggerFactory loggerFactory,
        RtpObserverInternal @internal,
        IChannel channel,
        AppData? appData,
        Func<string, Task<Producer.Producer?>> getProducerById
    )
        : base(loggerFactory, @internal, channel, appData, getProducerById, new())
    {
        logger = loggerFactory.CreateLogger<ActiveSpeakerObserver>();
    }

#pragma warning disable VSTHRD100 // Avoid async void methods
    protected override async void OnNotificationHandle(string handlerId, Event @event, Notification notification)
#pragma warning restore VSTHRD100 // Avoid async void methods
    {
        if(handlerId != Internal.RtpObserverId)
        {
            return;
        }

        switch(@event)
        {
            case Event.ACTIVESPEAKEROBSERVER_DOMINANT_SPEAKER:
            {
                var dominantSpeakerNotification = notification.BodyAsActiveSpeakerObserver_DominantSpeakerNotification().UnPack();

                var producer = await GetProducerById(dominantSpeakerNotification.ProducerId);
                if (producer != null)
                {
                    var dominantSpeaker = new ActiveSpeakerObserverDominantSpeaker
                    {
                        Producer = await GetProducerById(dominantSpeakerNotification.ProducerId)
                    };

                    Emit("dominantspeaker", dominantSpeaker);

                    // Emit observer event.
                    Observer.Emit("dominantspeaker", dominantSpeaker);
                }

                break;
            }
            default:
            {
                logger.LogError("OnNotificationHandle() | Ignoring unknown event: {Event}", @event);
                break;
            }
        }
    }
}