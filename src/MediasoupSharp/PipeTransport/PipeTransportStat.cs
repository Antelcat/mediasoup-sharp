using MediasoupSharp.DirectTransport;
using MediasoupSharp.Transport;

namespace MediasoupSharp.PipeTransport;

public record PipeTransportStat : DirectTransportStat
{
    // Common to all Transports.
    public SctpState? SctpState { get; set; }

    // PipeTransport specific.
    public TransportTuple Tuple { get; set; }
}