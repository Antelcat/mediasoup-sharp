using MediasoupSharp.RtpObserver;

namespace MediasoupSharp.AudioLevelObserver;

public record AudioLevelObserverObserverEvents : RtpObserverObserverEvents
{
    public List<List<AudioLevelObserverVolume>> Volumes = new();
    public List<object> Silence { get; set; } = new();
};