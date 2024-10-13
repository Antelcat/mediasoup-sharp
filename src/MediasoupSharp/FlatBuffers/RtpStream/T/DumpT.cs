using MediasoupSharp.FlatBuffers.RtxStream.T;

namespace MediasoupSharp.FlatBuffers.RtpStream.T;

public class DumpT
{
    public ParamsT Params { get; set; }

    public byte Score { get; set; }

    public RtxDumpT RtxStream { get; set; }
}
