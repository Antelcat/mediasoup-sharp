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

public abstract class ProducerEvents : BuiltInEvents
{
    public abstract object?                             TransportClose         { get; }
    public abstract List<ScoreT>                        Score                  { get; }
    public abstract VideoOrientationChangeNotificationT VideoOrientationChange { get; }
    public abstract TraceNotificationT                  Trace                  { get; }

    // Private events.
    internal abstract object? close { get; }
}

public abstract class ProducerObserverEvents
{
    public abstract object?                              Close                  { get; }
    public abstract object?                              Pause                  { get; }
    public abstract object?                              Resume                 { get; }
    public abstract List<ScoreT>?                        Score                  { get; }
    public abstract VideoOrientationChangeNotificationT? VideoOrientationChange { get; }
    public abstract TraceNotificationT?                  Trace                  { get; }
}

public interface IProducer<TProducerAppData> : IEnhancedEventEmitter<ProducerEvents>, IProducer
{
    TProducerAppData AppData { get; set; }
}