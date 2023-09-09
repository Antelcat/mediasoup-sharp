using MediasoupSharp.Transport;

namespace MediasoupSharp.PipeTransport;

public interface IPipeTransportData
{
    TransportTuple Tuple { get; set; }

    SctpParameters.SctpParameters? SctpParameters { get; set; }
    
    SctpState? SctpState { get; set; }

    bool Rtx { get; set; }

    SrtpParameters? SrtpParameters { get; set; }
}

public class PipeTransportData : IPipeTransportData
{
    public TransportTuple Tuple { get; set; }

    public SctpParameters.SctpParameters? SctpParameters { get; set; }
    
    public SctpState? SctpState { get; set; }

    public bool Rtx { get; set; }

    public SrtpParameters? SrtpParameters { get; set; }
}