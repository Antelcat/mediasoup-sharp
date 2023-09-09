namespace MediasoupSharp.RtpParameters;

public record RtpHeaderExtensionParameters
{
    /// <summary>
    /// The URI of the RTP header extension, as defined in RFC 5285.
    /// </summary>
    public string Uri { get; set; }

    /// <summary>
    /// The numeric identifier that goes in the RTP packet. Must be unique.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// If true, the value in the header is encrypted as per RFC 6904. Default false.
    /// </summary>
    public bool? Encrypt { get; set; }

    /// <summary>
    /// Configuration parameters for the header extension.
    /// </summary>
    public dynamic? Parameters { get; set; }
}