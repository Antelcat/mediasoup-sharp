using MediasoupSharp.Transport;

namespace MediasoupSharp.PipeTransport;

public record PipeTransportObserverEvents : TransportObserverEvents
{
    public Tuple<SctpState> Sctpstatechange { get; set; }
}