using MediasoupSharp.FlatBuffers.Transport.T;

namespace MediasoupSharp.FlatBuffers.PipeTransport.T;

public class PipeTransportOptionsT
{
    public global::FlatBuffers.Transport.OptionsT Base { get; set; }

    public ListenInfoT ListenInfo { get; set; }

    public bool EnableRtx { get; set; }

    public bool EnableSrtp { get; set; }
}
