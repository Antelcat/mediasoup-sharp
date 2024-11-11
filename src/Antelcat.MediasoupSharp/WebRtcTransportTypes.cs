global using WebRtcTransportObserver = Antelcat.MediasoupSharp.EnhancedEventEmitter<Antelcat.MediasoupSharp.WebRtcTransportObserverEvents>;
using Antelcat.AutoGen.ComponentModel;
using FBS.SctpAssociation;
using FBS.Transport;
using FBS.WebRtcTransport;

namespace Antelcat.MediasoupSharp;


[Serializable]
[AutoDeconstruct]
public partial record WebRtcTransportOptions<TWebRtcTransportAppData> 
    : WebRtcTransportOptionsBase<TWebRtcTransportAppData>
{
    /// <summary>
    /// Instance of WebRtcServer. Mandatory unless listenIps is given.
    /// </summary>
    public IWebRtcServer? WebRtcServer { get; set; }

    /// <summary>
    /// Listening IP address or addresses in order of preference (first one is the
    /// preferred one).
    /// </summary>
    public FBS.Transport.ListenInfoT[] ListenInfos { get; set; } = [];
}

public record WebRtcTransportOptionsBase<TWebRtcTransportAppData>
{
    /// <summary>
    /// Listen in UDP. Default true.
    /// </summary>
    public bool? EnableUdp { get; set; } = true;

    /// <summary>
    /// Listen in TCP. Default false.
    /// </summary>
    public bool? EnableTcp { get; set; }

    /// <summary>
    /// Prefer UDP. Default false.
    /// </summary>
    public bool PreferUdp { get; set; }

    /// <summary>
    /// Prefer TCP. Default false.
    /// </summary>
    public bool PreferTcp { get; set; }

    /// <summary>
    /// Initial available outgoing bitrate (in bps). Default 600000.
    /// </summary>
    public uint InitialAvailableOutgoingBitrate { get; set; } = 600000;

    /// <summary>
    /// Create a SCTP association. Default false.
    /// </summary>
    public bool EnableSctp { get; set; }

    /// <summary>
    /// SCTP streams number.
    /// </summary>
    public FBS.SctpParameters.NumSctpStreamsT? NumSctpStreams { get; set; } = new() { Os = 1024, Mis = 1024 };

    /// <summary>
    /// Maximum allowed size for SCTP messages sent by DataProducers.
    /// Default 262144.
    /// </summary>
    public uint MaxSctpMessageSize { get; init; } = 262144;

    /// <summary>
    /// Maximum SCTP send buffer used by DataConsumers.
    /// Default 262144.
    /// </summary>
    public uint SctpSendBufferSize { get; set; } = 262144;

    /// <summary>
    /// ICE consent timeout (in seconds). If 0 it is disabled. Default 30.
    /// </summary>
    public byte IceConsentTimeout { get; set; } = 30;

    /// <summary>
    /// Custom application data.
    /// </summary>
    public TWebRtcTransportAppData? AppData { get; set; }
}

public class WebRtcTransportListenServer
{
    /// <summary>
    /// Instance of WebRtcServer. Mandatory unless listenIps is given.
    /// </summary>
    public required IWebRtcServer WebRtcServer { get; set; }
}

public class WebRtcTransportListenIndividual
{
    /// <summary>
    /// Listening IP address or addresses in order of preference (first one is the
    /// preferred one).
    /// </summary>
    public ListenInfoT[]? ListenInfos { get; set; }

    /// <summary>
    /// Fixed port to listen on instead of selecting automatically from Worker's port
    /// range.
    /// </summary>
    public ushort? Port { get; set; } = 0; // mediasoup-work needs >= 0
}

public abstract class WebRtcTransportEvents : TransportEvents
{
    public required IceState  IceStateChange;
    public required TupleT    IceSelectedTupleChange;
    public required DtlsState DtlsStateChange;
    public required SctpState SctpStateChange;
}

public abstract class WebRtcTransportObserverEvents : TransportObserverEvents
{
    public required IceState  IceStateChange;
    public required TupleT    IceSelectedTupleChange;
    public required DtlsState DtlsStateChange;
    public required SctpState SctpStateChange;
}

public interface IWebRtcTransport<TWebRtcTransportAppData> :
    ITransport<TWebRtcTransportAppData, WebRtcTransportEvents, WebRtcTransportObserver>, IWebRtcTransport;