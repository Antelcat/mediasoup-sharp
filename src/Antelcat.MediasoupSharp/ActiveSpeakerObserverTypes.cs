global using ActiveSpeakerObserverObserver =
    Antelcat.MediasoupSharp.EnhancedEventEmitter<Antelcat.MediasoupSharp.ActiveSpeakerObserverObserverEvents>;

namespace Antelcat.MediasoupSharp;

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

public interface IActiveSpeakerObserver<TActiveSpeakerObserverAppData> :
    IRtpObserver<
        TActiveSpeakerObserverAppData,
        ActiveSpeakerObserverEvents,
        ActiveSpeakerObserverObserver
    >
    , IActiveSpeakerObserver;