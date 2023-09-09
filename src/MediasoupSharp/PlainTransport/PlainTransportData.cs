using MediasoupSharp.Transport;

namespace MediasoupSharp.PlainTransport;

public interface IPlainTransportData
{
    bool? RtcpMux { get; set; }

    bool? Comedia { get; set; }

    TransportTuple Tuple { get; set; }

    TransportTuple? RtcpTuple { get; set; }

    SctpParameters.SctpParameters? SctpParameters { get; set; }

    SctpState? SctpState { get; set; }

    SrtpParameters.SrtpParameters? SrtpParameters { get; set; }
}

public class PlainTransportData : IPlainTransportData
{
    public bool? RtcpMux { get; set; }

    public bool? Comedia { get; set; }

    public TransportTuple Tuple { get; set; }

    public TransportTuple? RtcpTuple { get; set; }

    public SctpParameters.SctpParameters? SctpParameters { get; set; }
    
    public SctpState? SctpState { get; set; }
    public SrtpParameters.SrtpParameters? SrtpParameters { get; set; }
}