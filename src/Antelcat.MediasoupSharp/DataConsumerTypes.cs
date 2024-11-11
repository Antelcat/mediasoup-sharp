global using DataConsumerObserver = Antelcat.MediasoupSharp.EnhancedEventEmitter<Antelcat.MediasoupSharp.DataConsumerObserverEvents>;
using FBS.DataConsumer;

namespace Antelcat.MediasoupSharp;


public class DataConsumerOptions<TDataConsumerAppData>
{
    /// <summary>
    /// The id of the DataProducer to consume.
    /// </summary>
    public required string DataProducerId { get; set; }

    /// <summary>
    /// Just if consuming over SCTP.
    /// Whether data messages must be received in order. If true the messages will
    /// be sent reliably. Defaults to the value in the DataProducer if it has type
    /// 'sctp' or to true if it has type 'direct'.
    /// </summary>
    public bool? Ordered { get; set; }

    /// <summary>
    /// Just if consuming over SCTP.
    /// When ordered is false indicates the time (in milliseconds) after which a
    /// SCTP packet will stop being retransmitted. Defaults to the value in the
    /// DataProducer if it has type 'sctp' or unset if it has type 'direct'.
    /// </summary>
    public int? MaxPacketLifeTime { get; set; }

    /// <summary>
    /// Just if consuming over SCTP.
    /// When ordered is false indicates the maximum number of times a packet will
    /// be retransmitted. Defaults to the value in the DataProducer if it has type
    /// 'sctp' or unset if it has type 'direct'.
    /// </summary>
    public int? MaxRetransmits { get; set; }

    /// <summary>
    /// Whether the data consumer must start in paused mode. Default false.
    /// </summary>
    /// <value></value>
    public bool Paused { get; set; }

    /**
     * Subchannels this data consumer initially subscribes to.
     * Only used in case this data consumer receives messages from a local data
     * producer that specifies subchannel(s) when calling send().
     */
    public List<ushort>? Subchannels { get; set; }

    /// <summary>
    /// Custom application data.
    /// </summary>
    public TDataConsumerAppData? AppData { get; set; }
}

public abstract class DataConsumerEvents
{
    public          object?              TransportClose;
    public          object?              DataProducerClose;
    public          object?              DataProducerPause;
    public          object?              DataProducerResume;
    public required MessageNotificationT Message;
    public          object?              SctpSendBufferFull;
    public          uint                 BufferedAmountLow;
    public          (string, Exception)  ListenerError;

    // Private events.
    internal object? close;
    internal object? dataProducerClose;
}

public abstract class DataConsumerObserverEvents
{
    public object? Close;
    public object? Pause;
    public object? Resume;
}

public interface IDataConsumer<TDataConsumerAppData>
    : IEnhancedEventEmitter<DataConsumerEvents>, IDataConsumer
{
    TDataConsumerAppData AppData { get; set; }
}