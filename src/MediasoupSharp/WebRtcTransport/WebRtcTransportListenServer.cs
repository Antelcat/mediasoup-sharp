namespace MediasoupSharp.WebRtcTransport;

public interface IWebRtcTransportListenServer
{
    /// <summary>
    /// Instance of WebRtcServer. Mandatory unless listenIps is given.
    /// </summary>
    public WebRtcServer.WebRtcServer WebRtcServer { get; set; }
}

public class WebRtcTransportListenServer : IWebRtcTransportListenServer
{
    /// <summary>
    /// Instance of WebRtcServer. Mandatory unless listenIps is given.
    /// </summary>
    public WebRtcServer.WebRtcServer WebRtcServer { get; set; }
}