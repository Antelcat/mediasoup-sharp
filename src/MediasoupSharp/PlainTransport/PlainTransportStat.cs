using MediasoupSharp.PipeTransport;
using MediasoupSharp.Transport;

namespace MediasoupSharp.PlainTransport;

public record PlainTransportStat : PipeTransportStat
{
    // PlainTransport specific.
    public bool           RtcpMux { get; set; }
    public bool           Comedia { get; set; }
    public TransportTuple? RtcpTuple { get; set; }
}