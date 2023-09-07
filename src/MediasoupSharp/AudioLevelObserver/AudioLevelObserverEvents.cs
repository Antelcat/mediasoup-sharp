using MediasoupSharp.RtpObserver;

namespace MediasoupSharp.AudioLevelObserver;

public class AudioLevelObserverEvents : RtpObserverEvents
{
    public List<List<AudioLevelObserverVolume>> Volumes = new();
    public List<object> Silence { get; set; } = new();
}