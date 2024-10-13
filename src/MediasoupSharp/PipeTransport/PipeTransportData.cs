using FlatBuffers.SrtpParameters;
using FlatBuffers.Transport;
using MediasoupSharp.Transport;

namespace MediasoupSharp.PipeTransport;

public class PipeTransportData : TransportBaseData
{
    public TupleT Tuple { get; set; }

    public bool Rtx { get; set; }

    public SrtpParametersT? SrtpParameters { get; set; }
}
