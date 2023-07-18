namespace MediasoupSharp;

public class RtpCapabilities
{
    
}

public record RtpCodecCapability(MediaKind Kind,
    string MimeType,
    Number ClockRate,
    Number Channels,
    dynamic Parameters);

public enum MediaKind
{
    Audio,
    Video,
}

public record RtcpFeedback(string Type, string Parameter);
