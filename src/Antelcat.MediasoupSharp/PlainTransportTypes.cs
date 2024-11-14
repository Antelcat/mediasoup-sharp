global using PlainTransportObserver = Antelcat.MediasoupSharp.EnhancedEventEmitter<Antelcat.MediasoupSharp.PlainTransportObserverEvents>;
using Antelcat.MediasoupSharp.FBS.SctpAssociation;
using Antelcat.MediasoupSharp.FBS.SctpParameters;
using Antelcat.MediasoupSharp.FBS.SrtpParameters;
using Antelcat.MediasoupSharp.FBS.Transport;

namespace Antelcat.MediasoupSharp;



public record PlainTransportOptions<TPlainTransportAppData>
{
    /// <summary>
    /// Listening information.
    /// </summary>
    public required ListenInfoT ListenInfo { get; set; }

    /// <summary>
    /// RTCP listening information. If not given and rtcpPort is not false,
    /// RTCP will use same listening info than RTP.
    /// </summary>
    public ListenInfoT? RtcpListenInfo { get; set; }

    /// <summary>
    /// Use RTCP-mux (RTP and RTCP in the same port). Default true.
    /// </summary>
    public bool? RtcpMux { get; set; } = true;

    /// <summary>
    /// Whether remote IP:port should be auto-detected based on first RTP/RTCP
    /// packet received. If enabled, connect() method must not be called unless
    /// SRTP is enabled. If so, it must be called with just remote SRTP parameters.
    /// Default false.
    /// </summary>
    public bool? Comedia { get; set; } = false;

    /// <summary>
    /// Create a SCTP association. Default false.
    /// </summary>
    public bool? EnableSctp { get; set; } = false;

    /// <summary>
    /// SCTP streams number.
    /// </summary>
    public NumSctpStreamsT? NumSctpStreams { get; set; }

    /// <summary>
    /// Maximum allowed size for SCTP messages sent by DataProducers.
    /// Default 262144.
    /// </summary>
    public uint? MaxSctpMessageSize { get; set; } = 262144;

    /// <summary>
    /// Maximum SCTP send buffer used by DataConsumers.
    /// Default 262144.
    /// </summary>
    public uint? SctpSendBufferSize { get; set; } = 262144;

    /// <summary>
    /// Enable SRTP. For this to work, connect() must be called
    /// with remote SRTP parameters. Default false.
    /// </summary>
    public bool? EnableSrtp { get; set; } = false;

    /// <summary>
    /// The SRTP crypto suite to be used if enableSrtp is set. Default
    /// 'AES_CM_128_HMAC_SHA1_80'.
    /// </summary>
    public SrtpCryptoSuite? SrtpCryptoSuite { get; set; } = Antelcat.MediasoupSharp.FBS.SrtpParameters.SrtpCryptoSuite.AES_CM_128_HMAC_SHA1_80;

    /// <summary>
    /// Custom application data.
    /// </summary>
    public TPlainTransportAppData? AppData { get; set; }
}

public abstract class PlainTransportEvents : TransportEvents
{
    public required TupleT    Tuple;
    public required TupleT    RtcpTuple;
    public required SctpState SctpStateChange;
}

public abstract class PlainTransportObserverEvents : TransportObserverEvents
{
    public required TupleT    Tuple;
    public required TupleT    RtcpTuple;
    public required SctpState SctpStateChange;
}

public interface IPlainTransport<TPlainTransportAppData>
    : ITransport<TPlainTransportAppData, PlainTransportEvents, PlainTransportObserver>, IPlainTransport;