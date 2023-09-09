namespace MediasoupSharp.RtpParameters;

public record RtpCodecCapability : RtpCodec
{
    public MediaKind Kind { get; set; }
    
    public int? PreferredPayloadType { get; set; }
};