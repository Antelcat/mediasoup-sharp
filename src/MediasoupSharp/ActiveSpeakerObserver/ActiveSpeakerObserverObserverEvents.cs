using MediasoupSharp.RtpObserver;

namespace MediasoupSharp.ActiveSpeakerObserver;

internal record ActiveSpeakerObserverObserverEvents : RtpObserverObserverEvents
{
    public List<ActiveSpeakerObserverDominantSpeaker> Dominantspeaker { get; set; }
}