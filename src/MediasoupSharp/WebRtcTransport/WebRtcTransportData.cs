
using MediasoupSharp.Transport;

namespace MediasoupSharp.WebRtcTransport;

public class WebRtcTransportData
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