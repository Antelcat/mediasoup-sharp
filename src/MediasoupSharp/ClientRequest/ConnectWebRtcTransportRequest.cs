using FBS.WebRtcTransport;

namespace MediasoupSharp.ClientRequest;

public class ConnectWebRtcTransportRequest : ConnectRequestT
{
    public string TransportId { get; set; }
}