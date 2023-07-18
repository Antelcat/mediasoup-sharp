namespace MediasoupSharp;

public record RtpParameters(string Mid,
    List<RtpCodecParameters> Codecs
);

public record RtpHeaderExtensionParameters(
    string Uri,
    Number Id,
    bool Encrypt,
    dynamic Parameters);

public record RtpCodecParameters(string MimeType,
    Number PayloadType,
    Number ClockRate,
    Number? Channels,
    dynamic? Parameters,
    List<RtcpFeedback>? RtcpFeedback);
    
public record RtcpParameters(
    string? Cname,
    bool? ReducedSize,
    bool? Mux);