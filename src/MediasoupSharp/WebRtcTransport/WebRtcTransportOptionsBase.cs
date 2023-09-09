using MediasoupSharp.SctpParameters;

namespace MediasoupSharp.WebRtcTransport;

public class WebRtcTransportOptionsBase<TWebRtcTransportAppData>
{
    /// <summary>
    /// Listen in UDP. Default true.
    /// </summary>
    public bool? EnableUdp { get; set; } = true;

    /// <summary>
    /// Listen in TCP. Default false.
    /// </summary>
    public bool? EnableTcp { get; set; } = false;

    /// <summary>
    /// Prefer UDP. Default false.
    /// </summary>
    public bool? PreferUdp { get; set; } = false;

    /// <summary>
    /// Prefer TCP. Default false.
    /// </summary>
    public bool? PreferTcp { get; set; } = false;

    /// <summary>
    /// Initial available outgoing bitrate (in bps). Default 600000.
    /// </summary>
    public int? InitialAvailableOutgoingBitrate { get; set; } = 600000;

    /// <summary>
    /// Create a SCTP association. Default false.
    /// </summary>
    public bool? EnableSctp { get; set; } = false;

    /// <summary>
    /// SCTP streams number.
    /// </summary>
    public NumSctpStreams? NumSctpStreams { get; set; } /*= new NumSctpStreams { OS = 1024, MIS = 1024 };*/

    /// <summary>
    /// Maximum allowed size for SCTP messages sent by DataProducers.
    /// Default 262144.
    /// </summary>
    public int? MaxSctpMessageSize { get; set; } = 262144;

    /// <summary>
    /// Maximum SCTP send buffer used by DataConsumers.
    /// Default 262144.
    /// </summary>
    public int? SctpSendBufferSize { get; set; } = 262144;

    /// <summary>
    /// Custom application data.
    /// </summary>
    public TWebRtcTransportAppData? AppData { get; set; }
}