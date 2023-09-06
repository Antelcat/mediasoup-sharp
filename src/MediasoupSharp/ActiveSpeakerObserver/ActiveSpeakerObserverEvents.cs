using MediasoupSharp.RtpObserver;

namespace MediasoupSharp.ActiveSpeakerObserver;

public class ActiveSpeakerObserverEvents : RtpObserverEvents
{
    public List<ActiveSpeakerObserverDominantSpeaker> Dominantspeaker { get; set; }
}