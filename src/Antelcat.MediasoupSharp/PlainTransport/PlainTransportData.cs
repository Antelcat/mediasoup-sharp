using Antelcat.MediasoupSharp.Transport;
using FBS.Transport;

namespace Antelcat.MediasoupSharp.PlainTransport;

public class PlainTransportData : TransportBaseData
{
    public bool? RtcpMux { get; set; }

    public bool? Comedia { get; set; }

    public TupleT Tuple { get; set; }

    public TupleT? RtcpTuple { get; set; }

    public TupleT? SrtpParameters { get; set; }
}