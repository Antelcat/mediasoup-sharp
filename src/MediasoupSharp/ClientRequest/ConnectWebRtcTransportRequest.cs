using MediasoupSharp.FlatBuffers.WebRtcTransport.T;

namespace MediasoupSharp.ClientRequest;

public class ConnectWebRtcTransportRequest : ConnectRequestT
{
    public string TransportId { get; set; }
}
