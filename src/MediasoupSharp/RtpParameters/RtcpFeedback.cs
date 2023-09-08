namespace MediasoupSharp.RtpParameters;

public record RtcpFeedback
{
    /// <summary>
    ///  RTCP feedback type.
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// RTCP feedback parameter.
    /// </summary>
    public string? Parameter { get; set; }
}