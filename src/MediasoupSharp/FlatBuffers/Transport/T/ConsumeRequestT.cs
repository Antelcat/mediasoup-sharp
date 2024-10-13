using MediasoupSharp.FlatBuffers.Consumer.T;
using MediasoupSharp.FlatBuffers.RtpParameters.T;

namespace MediasoupSharp.FlatBuffers.Transport.T;

public class ConsumeRequestT
{
    public string ConsumerId { get; set; }

    public string ProducerId { get; set; }

    public global::FlatBuffers.RtpParameters.MediaKind Kind { get; set; }

    public RtpParametersT RtpParameters { get; set; }

    public global::FlatBuffers.RtpParameters.Type Type { get; set; }

    public List<RtpEncodingParametersT> ConsumableRtpEncodings { get; set; }

    public bool Paused { get; set; }

    public ConsumerLayersT? PreferredLayers { get; set; }

    public bool IgnoreDtx { get; set; }
}