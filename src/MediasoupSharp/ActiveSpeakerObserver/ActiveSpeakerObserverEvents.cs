using MediasoupSharp.RtpObserver;

namespace MediasoupSharp.ActiveSpeakerObserver;

internal class ActiveSpeakerObserverEvents : RtpObserverEvents
{
    public List<ActiveSpeakerObserverDominantSpeaker> Dominantspeaker { get; set; }
}