using MediasoupSharp.RtpObserver;

namespace MediasoupSharp.ActiveSpeakerObserver;

public class ActiveSpeakerObserverObserverEvents : RtpObserverObserverEvents
{
    public List<ActiveSpeakerObserverDominantSpeaker> Dominantspeaker { get; set; }
}