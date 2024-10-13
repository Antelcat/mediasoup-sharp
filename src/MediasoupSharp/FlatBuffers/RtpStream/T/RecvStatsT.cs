namespace MediasoupSharp.FlatBuffers.RtpStream.T;

public class RecvStatsT
{
    public global::FlatBuffers.RtpStream.StatsT Base { get; set; }

    public uint Jitter { get; set; }

    public ulong PacketCount { get; set; }

    public ulong ByteCount { get; set; }

    public uint Bitrate { get; set; }

    public List<BitrateByLayerT> BitrateByLayer { get; set; }
}
