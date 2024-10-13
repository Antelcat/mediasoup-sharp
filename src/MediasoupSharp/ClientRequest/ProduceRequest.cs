using FlatBuffers.RtpParameters;

namespace MediasoupSharp.ClientRequest;

public class ProduceRequest
{
    public MediaKind Kind { get; set; }

    public RtpParameters.RtpParameters RtpParameters { get; set; }

    public string Source { get; set; }

    public Dictionary<string, object> AppData { get; set; }
}
