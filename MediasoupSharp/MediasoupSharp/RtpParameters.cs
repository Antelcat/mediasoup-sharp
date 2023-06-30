namespace MediasoupSharp;

public record RtpParameters(string mid,
    List<RtpCodecParameters> codecs
);

public record RtpHeaderExtensionParameters(
    string uri,
    Number id,
    bool encrypt,
    dynamic parameters);

public record RtpCodecParameters(string mimeType,
    Number payloadType,
    Number clockRate,
    Number? channels,
    dynamic? parameters,
    List<RtcpFeedback>? RtcpFeedback);
    
public record RtcpParameters(
    string? cname,
    bool? reducedSize,
    bool? mux);