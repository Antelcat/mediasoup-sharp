using MediasoupSharp.FlatBuffers.RtpPacket.T;

namespace MediasoupSharp.FlatBuffers.Producer.T;

public class KeyFrameTraceInfoT
{
    public DumpT RtpPacket { get; set; }

    public bool IsRtx { get; set; }
}
