namespace MediasoupSharp.FlatBuffers.WebRtcTransport.T;

public class IceCandidateT
{
    public string Foundation { get; set; }

    public uint Priority { get; set; }

    public string Ip { get; set; }

    public global::FlatBuffers.Transport.Protocol Protocol { get; set; }

    public ushort Port { get; set; }

    public global::FlatBuffers.WebRtcTransport.IceCandidateType Type { get; set; }

    public global::FlatBuffers.WebRtcTransport.IceCandidateTcpType? TcpType { get; set; }
}