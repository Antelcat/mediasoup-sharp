global using RtpObserverObserver =
    Antelcat.MediasoupSharp.IEnhancedEventEmitter<Antelcat.MediasoupSharp.RtpObserverObserverEvents>;

namespace Antelcat.MediasoupSharp;

public abstract class RtpObserverEvents
{
    public object? RouterClose;

    public required (string, Exception) ListenerError;

    // Private events.
    internal object? close;
}

public abstract class RtpObserverObserverEvents
{
    public          object?   Close;
    public          object?   Pause;
    public          object?   Resume;
    public required IProducer AddProducer;
    public required IProducer RemoveProducer;
}

public class RtpObserverConstructorOptions<TRtpObserverAppData>
{
    public required RtpObserverObserverInternal    Internal        { get; set; }
    public required IChannel                       Channel         { get; set; }
    public          TRtpObserverAppData?           AppData         { get; set; }
    public required Func<string, Task<IProducer?>> GetProducerById { get; set; }
}

public class RtpObserverObserverInternal : RouterInternal
{
    public required string RtpObserverId { get; set; }
}

public class RtpObserverAddRemoveProducerOptions
{
    /// <summary>
    /// The id of the Producer to be added or removed.
    /// </summary>
    public required string ProducerId { get; set; }
}

public interface IRtpObserver<TRtpObserverAppData, out TEvents, out TObserver>
    : IEnhancedEventEmitter<TEvents> , IRtpObserver
    where TEvents : RtpObserverEvents
    where TObserver : RtpObserverObserver
{
    TRtpObserverAppData AppData { get; set; }

    TObserver Observer { get; }
}