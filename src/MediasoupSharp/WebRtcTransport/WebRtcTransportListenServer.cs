using MediasoupSharp.WebRtcServer;

namespace MediasoupSharp.WebRtcTransport;

public interface IWebRtcTransportListenServer
{
    /// <summary>
    /// Instance of WebRtcServer. Mandatory unless listenIps is given.
    /// </summary>
    IWebRtcServer WebRtcServer { get; set; }
}

public class WebRtcTransportListenServer : IWebRtcTransportListenServer
{
    /// <summary>
    /// Instance of WebRtcServer. Mandatory unless listenIps is given.
    /// </summary>
    public IWebRtcServer WebRtcServer { get; set; }
}