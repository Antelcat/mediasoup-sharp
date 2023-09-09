using MediasoupSharp.Transport;

namespace MediasoupSharp.WebRtcTransport;

public record WebRtcTransportConstructorOptions<TWebRtcTransportAppData>
    : TransportConstructorOptions<TWebRtcTransportAppData>
{
    public WebRtcTransportData Data { get; set; }
}