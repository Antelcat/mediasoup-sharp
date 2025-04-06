global using TransportObserver = Antelcat.MediasoupSharp.IEnhancedEventEmitter<Antelcat.MediasoupSharp.TransportObserverEvents>;

using Antelcat.MediasoupSharp.FBS.Transport;

namespace Antelcat.MediasoupSharp;


public abstract class TransportEvents : BuiltInEvents
{
    public abstract object?            RouterClose       { get; }
    public abstract object?            ListenServerClose { get; }
    public abstract TraceNotificationT Trace             { get; }

    // Private events.
    internal abstract object?       close             { get; }
    internal abstract IProducer     newProducer       { get; }
    internal abstract IProducer     producerClose     { get; }
    internal abstract IDataProducer newDataProducer   { get; }
    internal abstract IDataProducer dataProducerClose { get; }
    internal abstract object?       listenServerClose { get; }
}

public abstract class TransportObserverEvents
{
    public abstract object?            Close           { get; }
    public abstract IProducer          NewProducer     { get; }
    public abstract IConsumer          NewConsumer     { get; }
    public abstract IDataProducer      NewDataProducer { get; }
    public abstract IDataConsumer      NewDataConsumer { get; }
    public abstract TraceNotificationT Trace           { get; }
}

public interface ITransport<TTransportAppData, out TEvents, out TObserver>
    : IEnhancedEventEmitter<TEvents>, ITransport
    where TEvents : TransportEvents
    where TObserver : TransportObserver
{
    TTransportAppData AppData  { get; set; }
    public TObserver  Observer { get; }
}