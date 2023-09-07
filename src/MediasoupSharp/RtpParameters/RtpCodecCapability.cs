namespace MediasoupSharp.RtpParameters;

public record RtpCodecCapability
{
    public MediaKind Kind { get; set; }

    public string MimeType { get; set; } = string.Empty;
    
    public int? PreferredPayloadType { get; set; }
    
    public int ClockRate { get; set; }
    
    public int? Channels { get; set; }
    
    public object? Parameters { get; set; }
    
    public List<RtcpFeedback>? RtcpFeedback { get; set; }
};