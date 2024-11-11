global using TransportObserver = Antelcat.MediasoupSharp.IEnhancedEventEmitter<Antelcat.MediasoupSharp.TransportObserverEvents>;

using FBS.Transport;

namespace Antelcat.MediasoupSharp;


public abstract class TransportEvents
{
    public          object?            RouterClose;
    public          object?            ListenServerClose;
    public required TraceNotificationT Trace;

    public (string, Exception)? ListenerError;

    // Private events.
    internal object?       close;
    internal IProducer     newProducer;
    internal IProducer     producerClose;
    internal IDataProducer newDataProducer;
    internal IDataProducer dataProducerClose;
    internal object?       listenServerClose;
}

public abstract class TransportObserverEvents
{
    public          object?            Close;
    public required IProducer          NewProducer;
    public required IConsumer          NewConsumer;
    public required IDataProducer      NewDataProducer;
    public required IDataConsumer      NewDataConsumer;
    public required TraceNotificationT Trace;
}

public interface ITransport<TTransportAppData, out TEvents, out TObserver>
    : IEnhancedEventEmitter<TEvents>, ITransport
    where TEvents : TransportEvents
    where TObserver : TransportObserver
{

    TTransportAppData AppData  { get; set; }
    public TObserver  Observer { get; }
}