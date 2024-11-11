global using AudioLevelObserverObserver = Antelcat.MediasoupSharp.EnhancedEventEmitter<Antelcat.MediasoupSharp.AudioLevelObserverObserverEvents>;

namespace Antelcat.MediasoupSharp;

public class AudioLevelObserverOptions<TAudioLevelObserverAppData>
{
    /// <summary>
    /// Maximum number of entries in the 'volumes”' event. Default 1.
    /// </summary>
    public ushort MaxEntries { get; set; } = 1;

    /// <summary>
    /// Minimum average volume (in dBvo from -127 to 0) for entries in the
    /// 'volumes' event. Default -80.
    /// </summary>
    public sbyte Threshold { get; set; } = -80;

    /// <summary>
    /// Interval in ms for checking audio volumes. Default 1000.
    /// </summary>
    public ushort Interval { get; set; } = 1000;

    /// <summary>
    /// Custom application data.
    /// </summary>
    public TAudioLevelObserverAppData? AppData { get; set; }
}

public class AudioLevelObserverVolume
{
    /// <summary>
    /// The audio Producer instance.
    /// </summary>
    public required IProducer Producer { get; set; }

    /// <summary>
    /// The average volume (in dBvo from -127 to 0) of the audio Producer in the
    /// last interval.
    /// </summary>
    public int Volume { get; set; }
}

public abstract class AudioLevelObserverEvents : RtpObserverEvents
{
    public required List<AudioLevelObserverVolume> Volumes;
    public          object?                        Silence;
}


public abstract class AudioLevelObserverObserverEvents : RtpObserverObserverEvents
{
    public required List<AudioLevelObserverVolume> Volumes;
    public          object?                        Silence;
}

public class AudioLevelObserverConstructorOptions<TAudioLevelObserverAppData>
    : RtpObserverConstructorOptions<TAudioLevelObserverAppData>;

public interface IAudioLevelObserver<TAudioLevelObserverAppData> :
    IRtpObserver<
        TAudioLevelObserverAppData,
        AudioLevelObserverEvents, 
        AudioLevelObserverObserver
    >
    , IAudioLevelObserver;