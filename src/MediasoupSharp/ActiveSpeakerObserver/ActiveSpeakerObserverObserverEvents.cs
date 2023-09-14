using MediasoupSharp.RtpObserver;

namespace MediasoupSharp.ActiveSpeakerObserver;

public record ActiveSpeakerObserverObserverEvents : RtpObserverObserverEvents
{
    public List<ActiveSpeakerObserverDominantSpeaker> Dominantspeaker { get; set; }
}