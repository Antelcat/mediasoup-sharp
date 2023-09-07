namespace MediasoupSharp.RtpParameters;

public class RtpCapabilities
{
    List<RtpCodecCapability>? Codecs { get; set; }
    
    List<RtpHeaderExtension>? HeaderExtensions { get; set; }
}