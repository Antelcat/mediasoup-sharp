namespace MediasoupSharp.RtpParameters;

public record RtpParameters
{
    /// <summary>
    /// The MID RTP extension value as defined in the BUNDLE specification.
    /// </summary>
    public string? Mid { get; set; }

    /// <summary>
    /// Media and RTX codecs in use.
    /// </summary>
    public List<RtpCodecParameters> Codecs { get; set; } = new();

    /// <summary>
    /// RTP header extensions in use.
    /// </summary>
    public List<RtpHeaderExtensionParameters>? HeaderExtensions { get; set; }

    /// <summary>
    /// Transmitted RTP streams and their settings.
    /// </summary>
    public List<RtpEncodingParameters>? Encodings { get; set; }

    /// <summary>
    /// Parameters used for RTCP.
    /// </summary>
    public RtcpParameters? Rtcp { get; set; }
}