using MediasoupSharp.FlatBuffers.RtpParameters.T;

namespace MediasoupSharp.FlatBuffers.Consumer.T;

public class BaseConsumerDumpT
{
    public string Id { get; set; }

    public global::FlatBuffers.RtpParameters.Type Type { get; set; }

    public string ProducerId { get; set; }

    public global::FlatBuffers.RtpParameters.MediaKind Kind { get; set; }

    public RtpParametersT RtpParameters { get; set; }

    public List<RtpEncodingParametersT> ConsumableRtpEncodings { get; set; }

    public List<byte> SupportedCodecPayloadTypes { get; set; }

    public List<global::FlatBuffers.Consumer.TraceEventType> TraceEventTypes { get; set; }

    public bool Paused { get; set; }

    public bool ProducerPaused { get; set; }

    public byte Priority { get; set; }
}
