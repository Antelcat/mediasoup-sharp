using MediasoupSharp.FlatBuffers.RtpPacket.T;

namespace MediasoupSharp.FlatBuffers.Consumer.T;

public class RtpTraceInfoT
{
    public DumpT RtpPacket { get; set; }

    public bool IsRtx { get; set; }
}
