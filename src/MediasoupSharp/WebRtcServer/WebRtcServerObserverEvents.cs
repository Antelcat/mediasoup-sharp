using MediasoupSharp.WebRtcTransport;

namespace MediasoupSharp.WebRtcServer;

public record WebRtcServerObserverEvents
{
    public   List<object>                           Close                    { get; set; } = new();
    internal Tuple<WebRtcTransport.WebRtcTransport> Webrtctransporthandled   { get; set; }
    internal Tuple<WebRtcTransport.WebRtcTransport> Webrtctransportunhandled { get; set; }
}