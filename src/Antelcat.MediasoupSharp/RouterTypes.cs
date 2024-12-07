global using RouterObserver = Antelcat.MediasoupSharp.EnhancedEventEmitter<Antelcat.MediasoupSharp.RouterObserverEvents>;
using Antelcat.MediasoupSharp.FBS.SctpParameters;
using Antelcat.MediasoupSharp.FBS.Transport;

namespace Antelcat.MediasoupSharp;


public class RouterOptions<TRouterAppData>
{
    /// <summary>
    /// Router media codecs.
    /// </summary>
    public RtpCodecCapability[] MediaCodecs { get; set; } = [];

    /// <summary>
    /// Custom application data.
    /// </summary>
    public TRouterAppData? AppData { get; set; }
}

public class PipeToRouterOptions
{
    /// <summary>
    /// The id of the Producer to consume.
    /// </summary>
    public string? ProducerId { get; set; }

    /// <summary>
    /// The id of the DataProducer to consume.
    /// </summary>
    public string? DataProducerId { get; set; }

    /// <summary>
    /// Target Router instance.
    /// </summary>
    public required IRouter Router { get; set; }

    /// <summary>
    /// Listening Information.
    /// </summary>
    public required ListenInfoT ListenInfo { get; set; }

    /// <summary>
    /// Create a SCTP association. Default true.
    /// </summary>
    public bool EnableSctp { get; set; } = true;

    /// <summary>
    /// SCTP streams number.
    /// </summary>
    public NumSctpStreamsT NumSctpStreams { get; set; } = new() { Os = 1024, Mis = 1024 };

    /// <summary>
    /// Enable RTX and NACK for RTP retransmission.
    /// </summary>
    public bool EnableRtx { get; set; }

    /// <summary>
    /// Enable SRTP.
    /// </summary>
    public bool EnableSrtp { get; set; }
}

public class PipeToRouterResult
{
    /// <summary>
    /// The Consumer created in the current Router.
    /// </summary>
    public IConsumer? PipeConsumer { get; set; }

    /// <summary>
    /// The Producer created in the target Router.
    /// </summary>
    public IProducer? PipeProducer { get; set; }

    /// <summary>
    /// The DataConsumer created in the current Router.
    /// </summary>
    public IDataConsumer? PipeDataConsumer { get; set; }

    /// <summary>
    /// The DataProducer created in the target Router.
    /// </summary>
    public IDataProducer? PipeDataProducer { get; set; }
}

public abstract class RouterEvents
{
    public object? WorkerClose;

    public (string eventName, Exception error) ListenerError;

    // Private events.
    internal object? close;
}

public abstract class RouterObserverEvents
{
    public          object?      Close;
    public required ITransport   NewTransport;
    public required IRtpObserver NewRtpObserver;
}

public interface IRouter<TRouterAppData> : IEnhancedEventEmitter<RouterEvents>, IRouter
{
    TRouterAppData AppData { get; set; }
}