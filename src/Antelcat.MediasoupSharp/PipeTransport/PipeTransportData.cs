using Antelcat.MediasoupSharp.Transport;
using FBS.SrtpParameters;
using FBS.Transport;

namespace Antelcat.MediasoupSharp.PipeTransport;

public class PipeTransportData : TransportBaseData
{
    public TupleT Tuple { get; set; }

    public bool Rtx { get; set; }

    public SrtpParametersT? SrtpParameters { get; set; }
}