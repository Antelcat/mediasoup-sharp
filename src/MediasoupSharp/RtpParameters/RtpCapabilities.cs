using System.Dynamic;

namespace MediasoupSharp.RtpParameters;

public class RtpCapabilities
{
    public List<RtpCodecCapability>? Codecs { get; set; }

    public List<RtpHeaderExtension>? HeaderExtensions { get; set; }
}