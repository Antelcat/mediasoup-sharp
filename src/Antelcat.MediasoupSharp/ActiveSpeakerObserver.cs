﻿using Antelcat.AutoGen.ComponentModel.Diagnostic;
using FBS.Notification;
using Microsoft.Extensions.Logging;

namespace Antelcat.MediasoupSharp;

using ActiveSpeakerObserverObserver = EnhancedEventEmitter<ActiveSpeakerObserverObserverEvents>;

public class ActiveSpeakerObserverOptions<TActiveSpeakerObserverAppData>
{
    /// <summary>
    /// Interval in ms for checking audio volumes. Default 300.
    /// </summary>
    public ushort Interval { get; set; } = 300;

    /// <summary>
    /// Custom application data.
    /// </summary>
    public TActiveSpeakerObserverAppData? AppData { get; set; }
}

public class ActiveSpeakerObserverDominantSpeaker
{
    /// <summary>
    /// The producer instance.
    /// </summary>
    public IProducer? Producer { get; init; }
}

public abstract class ActiveSpeakerObserverEvents : RtpObserverEvents
{
    public required ActiveSpeakerObserverDominantSpeaker DominantSpeaker;
}

public abstract class ActiveSpeakerObserverObserverEvents : RtpObserverObserverEvents
{
    public required ActiveSpeakerObserverDominantSpeaker DominantSpeaker;
}

public class RtpObserverObserverConstructorOptions<TActiveSpeakerObserverAppData> :
    RtpObserverConstructorOptions<TActiveSpeakerObserverAppData>;

[AutoExtractInterface]
public class ActiveSpeakerObserver<TActiveSpeakerObserverAppData>
    : RtpObserver<TActiveSpeakerObserverAppData, ActiveSpeakerObserverEvents, ActiveSpeakerObserverObserver>, IActiveSpeakerObserver
    where TActiveSpeakerObserverAppData :  new()
{
    /// <summary>
    /// Logger.
    /// </summary>
    private readonly ILogger logger = new Logger<ActiveSpeakerObserver<TActiveSpeakerObserverAppData>>();

    /// <summary>
    /// <para>Events:</para>
    /// <para>@emits <see cref="ActiveSpeakerObserverEvents.DominantSpeaker"/></para>
    /// <para>Observer events:</para>
    /// <para>@emits <see cref="ActiveSpeakerObserverObserverEvents.DominantSpeaker"/></para>
    /// </summary>
    public ActiveSpeakerObserver(RtpObserverObserverConstructorOptions<TActiveSpeakerObserverAppData> options)
        : base(options, new())
    {
    }

#pragma warning disable VSTHRD100 // Avoid async void methods
    protected override async void OnNotificationHandle(string handlerId, Event @event, Notification notification)
#pragma warning restore VSTHRD100 // Avoid async void methods
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

                    this.Emit(static x => x.DominantSpeaker, dominantSpeaker);

                    // Emit observer event.
                    Observer.Emit(static x => x.DominantSpeaker, dominantSpeaker);
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
}