using System.ComponentModel.DataAnnotations;
using FlatBuffers.Transport;

namespace MediasoupSharp.FlatBuffers.Transport.T;

public class ListenInfoT
{
    public Protocol Protocol { get; set; }

    [Required]
    public string Ip { get; set; }

    public string? AnnouncedIp { get; set; }

    public ushort Port { get; set; }

    [Required]
    public SocketFlagsT Flags { get; set; } = new() { Ipv6Only = false, UdpReusePort = false };

    public uint SendBufferSize { get; set; }

    public uint RecvBufferSize { get; set; }
}