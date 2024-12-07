global using ProducerObserver = Antelcat.MediasoupSharp.EnhancedEventEmitter<Antelcat.MediasoupSharp.ProducerObserverEvents>;
using Antelcat.MediasoupSharp.FBS.Producer;
using Antelcat.MediasoupSharp.FBS.RtpParameters;

namespace Antelcat.MediasoupSharp;

public class ProducerOptions<TProducerAppData>
{
    /// <summary>
    /// Producer id (just for Router.pipeToRouter() method).
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// Media kind ('audio' or 'video').
    /// </summary>
    public MediaKind Kind { get; set; }

    /// <summary>
    /// RTP parameters defining what the endpoint is sending.
    /// </summary>
    public required RtpParameters RtpParameters { get; set; }

    /// <summary>
    /// Whether the producer must start in paused mode. Default false.
    /// </summary>
    public bool Paused { get; set; }

    /// <summary>
    /// Just for video. Time (in ms) before asking the sender for a new key frame
    /// after having asked a previous one. Default 0.
    /// </summary>
    public uint KeyFrameRequestDelay { get; set; }

    /// <summary>
    /// Custom application data.
    /// </summary>
    public TProducerAppData? AppData { get; set; }
}

public abstract class ProducerEvents
{
    public          object?                             TransportClose;
    public required List<ScoreT>                        Score;
    public required VideoOrientationChangeNotificationT VideoOrientationChange;
    public required TraceNotificationT                  Trace;

    public (string eventName, Exception error) ListenerError;

    // Private events.
    internal object? close;
}

public abstract class ProducerObserverEvents
{
    public object?                              Close;
    public object?                              Pause;
    public object?                              Resume;
    public List<ScoreT>?                        Score;
    public VideoOrientationChangeNotificationT? VideoOrientationChange;
    public TraceNotificationT?                  Trace;
}

public interface IProducer<TProducerAppData> : IEnhancedEventEmitter<ProducerEvents>, IProducer
{
    TProducerAppData AppData { get; set; }
}