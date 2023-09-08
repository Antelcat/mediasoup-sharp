﻿namespace MediasoupSharp.Transport;

public record TransportListenIp
{
    /// <summary>
    /// Listening IPv4 or IPv6.
    /// </summary>
    public string Ip { get; set; }
    
    /// <summary>
    /// Announced IPv4 or IPv6 (useful when running mediasoup behind NAT with
    /// private IP).
    /// </summary>
    public string? AnnouncedIp { get; set; }

}