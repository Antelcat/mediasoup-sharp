using MediasoupSharp.Transport;

namespace MediasoupSharp.WebRtcTransport;

public record IceCandidate
{
    public string Foundation { get; set; }
    public int Priority { get; set; }
    public string Ip { get; set; }
    public TransportProtocol Protocol { get; set; }
    public int Port { get; set; }
    public string Type { get; set; } = "host";
    /// <summary>
    /// "passive" | null
    /// </summary>
    public string? TcpType { get; set; }
}