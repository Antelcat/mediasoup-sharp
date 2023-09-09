namespace MediasoupSharp.WebRtcTransport;


public interface IWebRtcTransportListen : IWebRtcTransportListenIndividual, IWebRtcTransportListenServer{}

public record WebRtcTransportListen : IWebRtcTransportListen
{
    public List<object> ListenIps { get; set; }
    public ushort? Port { get; set; }
    public WebRtcServer.WebRtcServer WebRtcServer { get; set; }
}
