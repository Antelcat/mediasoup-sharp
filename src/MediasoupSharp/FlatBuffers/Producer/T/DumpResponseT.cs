using MediasoupSharp.FlatBuffers.RtpParameters.T;
using MediasoupSharp.FlatBuffers.RtpStream.T;

namespace MediasoupSharp.FlatBuffers.Producer.T;

public class DumpResponseT
{
    public string Id { get; set; }

    public global::FlatBuffers.RtpParameters.MediaKind Kind { get; set; }

    public global::FlatBuffers.RtpParameters.Type Type { get; set; }

    public RtpParametersT RtpParameters { get; set; }

    public RtpMappingT RtpMapping { get; set; }

    public List<DumpT> RtpStreams { get; set; }

    public List<global::FlatBuffers.Producer.TraceEventType> TraceEventTypes { get; set; }

    public bool Paused { get; set; }
}
