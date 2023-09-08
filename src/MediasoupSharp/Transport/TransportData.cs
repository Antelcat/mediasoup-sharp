using MediasoupSharp.DirectTransport;
using MediasoupSharp.PipeTransport;
using MediasoupSharp.PlainTransport;
using MediasoupSharp.WebRtcTransport;

namespace MediasoupSharp.Transport;

public class TransportData : WebRtcTransportData, PlainTransportData, PipeTransportData, DirectTransportData
{
    
}