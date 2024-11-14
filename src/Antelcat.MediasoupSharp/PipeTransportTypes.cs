global using PipeTransportObserver = Antelcat.MediasoupSharp.EnhancedEventEmitter<Antelcat.MediasoupSharp.PipeTransportObserverEvents>;
using Antelcat.MediasoupSharp.FBS.SctpAssociation;
using Antelcat.MediasoupSharp.FBS.SctpParameters;
using Antelcat.MediasoupSharp.FBS.Transport;

namespace Antelcat.MediasoupSharp;

public class PipeTransportOptions<TPipeTransportAppData>
{
    /// <summary>
    /// Listening Information.
    /// </summary>
    public required ListenInfoT ListenInfo { get; set; }

    /// <summary>
    /// Create a SCTP association. Default false.
    /// </summary>
    public bool EnableSctp { get; set; }

    /// <summary>
    /// SCTP streams number.
    /// </summary>
    public NumSctpStreamsT NumSctpStreams { get; set; } = new() { Os = 1024, Mis = 1024 };

    /// <summary>
    /// Maximum allowed size for SCTP messages sent by DataProducers.
    /// Default 268435456.
    /// </summary>
    public uint MaxSctpMessageSize { get; set; } = 268435456;

    /// <summary>
    /// Maximum SCTP send buffer used by DataConsumers.
    /// Default 268435456.
    /// </summary>
    public uint SctpSendBufferSize { get; set; } = 268435456;

    /// <summary>
    /// Enable RTX and NACK for RTP retransmission. Useful if both Routers are
    /// located in different hosts and there is packet lost in the link. For this
    /// to work, both PipeTransports must enable this setting. Default false.
    /// </summary>
    public bool EnableRtx { get; set; }

    /// <summary>
    /// Enable SRTP. Useful to protect the RTP and RTCP traffic if both Routers
    /// are located in different hosts. For this to work, connect() must be called
    /// with remote SRTP parameters. Default false.
    /// </summary>
    public bool EnableSrtp { get; set; }

    /// <summary>
    /// Custom application data.
    /// </summary>
    public TPipeTransportAppData? AppData { get; set; }
}

public class PipeConsumerOptions<TConsumerAppData>
{
    /// <summary>
    /// The id of the Producer to consume.
    /// </summary>
    public required string ProducerId { get; set; }

    /// <summary>
    /// Custom application data.
    /// </summary>
    public TConsumerAppData? AppData { get; set; }
}

public abstract class PipeTransportEvents : TransportEvents
{
    public required SctpState SctpStateChange;
}

public abstract class PipeTransportObserverEvents : TransportObserverEvents
{
    public required SctpState SctpStateChange;
}


public interface IPipeTransport<TPipeTransportAppData>
    : ITransport<TPipeTransportAppData, PipeTransportEvents, PipeTransportObserver>, IPipeTransport;