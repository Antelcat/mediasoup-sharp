using MediasoupSharp.WebRtcTransport;

namespace MediasoupSharp.WebRtcServer;

public record WebRtcServerObserverEvents
{
    public   List<object>                           Close                    { get; set; } = new();
    internal Tuple<IWebRtcTransport> Webrtctransporthandled   { get; set; }
    internal Tuple<IWebRtcTransport> Webrtctransportunhandled { get; set; }
}