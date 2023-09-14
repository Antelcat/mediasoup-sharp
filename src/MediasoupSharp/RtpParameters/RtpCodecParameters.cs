namespace MediasoupSharp.RtpParameters;

public record RtpCodecParameters : RtpCodec
{
    /// <summary>
    /// The value that goes in the RTP Payload Type Field. Must be unique.
    /// </summary>
    public int PayloadType { get; set; }
}