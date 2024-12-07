global using DataProducerObserver = Antelcat.MediasoupSharp.EnhancedEventEmitter<Antelcat.MediasoupSharp.DataProducerObserverEvents>;

namespace Antelcat.MediasoupSharp;

public class DataProducerOptions<TDataProducerAppData>
{
    /// <summary>
    /// DataProducer id (just for Router.pipeToRouter() method).
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// SCTP parameters defining how the endpoint is sending the data.
    /// </summary>
    public SctpStreamParameters? SctpStreamParameters { get; set; }

    /// <summary>
    /// A label which can be used to distinguish this DataChannel from others.
    /// </summary>
    public string? Label { get; set; }

    /// <summary>
    /// Name of the sub-protocol used by this DataChannel.
    /// </summary>
    public string? Protocol { get; set; }

    /// <summary>
    /// Whether the data producer must start in paused mode. Default false.
    /// </summary>
    public bool Paused { get; set; }

    /// <summary>
    /// Custom application data.
    /// </summary>
    public TDataProducerAppData? AppData { get; set; }
}

public abstract class DataProducerEvents
{
    public object? TransportClose;

    public (string eventName, Exception error) ListenerError;

    // Private events.
    internal object? close;
}

public abstract class DataProducerObserverEvents
{
    public object? Close;
    public object? Pause;
    public object? Resume;
}

public interface IDataProducer<TDataConsumerAppData>
    : IEnhancedEventEmitter<DataConsumerEvents>, IDataProducer
{
    TDataConsumerAppData AppData { get; set; }
}