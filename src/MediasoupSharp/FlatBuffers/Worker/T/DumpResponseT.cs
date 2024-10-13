using MediasoupSharp.FlatBuffers.LibUring.T;

namespace MediasoupSharp.FlatBuffers.Worker.T;

public class DumpResponseT
{
    public uint Pid { get; set; }

    public List<string> WebRtcServerIds { get; set; }

    public List<string> RouterIds { get; set; }

    public ChannelMessageHandlersT ChannelMessageHandlers { get; set; }

    public DumpT Liburing { get; set; }
}