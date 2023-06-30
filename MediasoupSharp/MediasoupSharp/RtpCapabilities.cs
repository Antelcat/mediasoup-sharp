namespace MediasoupSharp;

public class RtpCapabilities
{
    
}

public record RtpCodecCapability(MediaKind kind,
    string mimeType,
    Number clockRate,
    Number channels,
    dynamic parameters);

public enum MediaKind
{
    audio,
    video,
}

public record RtcpFeedback(string type, string parameter);
