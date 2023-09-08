using MediasoupSharp.Transport;

namespace MediasoupSharp.PipeTransport;

public class PipeTransportData : TransportBaseData
{
    public TransportTuple Tuple { get; set; }

    public bool Rtx { get; set; }

    public SrtpParameters? SrtpParameters { get; set; }
}