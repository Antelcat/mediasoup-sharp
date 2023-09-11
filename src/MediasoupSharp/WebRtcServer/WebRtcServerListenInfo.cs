using MediasoupSharp.Transport;

namespace MediasoupSharp.WebRtcServer;

public record WebRtcServerListenInfo
{
    /// <summary>
    /// Network protocol.
    /// </summary>
    public TransportProtocol Protocol { get; set; }

    /// <summary>
    /// Listening IPv4 or IPv6.
    /// </summary>
    public string Ip { get; set; }

    /// <summary>
    /// Announced IPv4 or IPv6 (useful when running mediasoup behind NAT with
    /// private IP).
    /// </summary>
    public string? AnnouncedIp { get; set; }

    /// <summary>
    /// Listening port.
    /// </summary>
    public int? Port { get; set; }
}