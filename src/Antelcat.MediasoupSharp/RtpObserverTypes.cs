global using RtpObserverObserver =
    Antelcat.MediasoupSharp.IEnhancedEventEmitter<Antelcat.MediasoupSharp.RtpObserverObserverEvents>;

namespace Antelcat.MediasoupSharp;

public abstract class RtpObserverEvents : BuiltInEvents
{
    public abstract object? RouterClose { get; }

    // Private events.
    internal abstract object? close { get; }
}

public abstract class RtpObserverObserverEvents
{
    public abstract object?   Close          { get; }
    public abstract object?   Pause          { get; }
    public abstract object?   Resume         { get; }
    public abstract IProducer AddProducer    { get; }
    public abstract IProducer RemoveProducer { get; }
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