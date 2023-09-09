
using MediasoupSharp.Transport;

namespace MediasoupSharp.WebRtcTransport;

public interface IWebRtcTransportData
{
    string IceRole { get; set; }

    IceParameters IceParameters { get; set; }

    List<IceCandidate> IceCandidates { get; set; } 

    IceState IceState { get; set; }

    TransportTuple? IceSelectedTuple { get; set; }

    DtlsParameters DtlsParameters { get; set; }

    DtlsState DtlsState { get; set; }

    string? DtlsRemoteCert { get; set; }
    
    SctpParameters.SctpParameters? SctpParameters { get; set; }
    
    SctpState? SctpState { get; set; }
}

public class WebRtcTransportData : IWebRtcTransportData
{
    public string IceRole { get; set; } = "controlled";

    public IceParameters IceParameters { get; set; }

    public List<IceCandidate> IceCandidates { get; set; } = new();

    public IceState IceState { get; set; }

    public TransportTuple? IceSelectedTuple { get; set; }

    public DtlsParameters DtlsParameters { get; set; }

    public DtlsState DtlsState { get; set; }

    public string? DtlsRemoteCert { get; set; }

    public SctpParameters.SctpParameters? SctpParameters { get; set; }

    public SctpState? SctpState { get; set; }
}