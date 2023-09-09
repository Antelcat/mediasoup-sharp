using MediasoupSharp.Transport;

namespace MediasoupSharp.PipeTransport;

public record PipeTransportEvents : TransportEvents
{
    public Tuple<SctpState> Sctpstatechange { get; set; }
}