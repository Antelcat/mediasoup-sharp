using MediasoupSharp.Producer;

namespace MediasoupSharp.ActiveSpeakerObserver;

public record ActiveSpeakerObserverDominantSpeaker
{
    public IProducer Producer { get; set; }
}