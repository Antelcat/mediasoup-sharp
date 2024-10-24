using FBS.WebRtcTransport;

namespace Antelcat.MediasoupSharp.ClientRequest;

public class ConnectWebRtcTransportRequest : ConnectRequestT
{
    public string TransportId { get; set; }
}