using System.Dynamic;

namespace MediasoupSharp.RtpParameters;

public record RtpCodec
{
    /// <summary>
    /// The codec MIME media type/subtype (e.g. 'audio/opus', 'video/VP8').
    /// </summary>
    public string MimeType { get; set; } = string.Empty;
    
    /// <summary>
    /// Codec clock rate expressed in Hertz.
    /// </summary>
    public int ClockRate { get; set; }
    
    /// <summary>
    /// The number of channels supported (e.g. two for stereo). Just for audio.
    /// Default 1.
    /// </summary>
    public int? Channels { get; set; } = 1;
    
    /// <summary>
    /// Codec-specific parameters available for signaling. Some parameters (such
    /// as 'packetization-mode' and 'profile-level-id' in H264 or 'profile-id' in
    /// VP9) are critical for codec matching.
    /// </summary>
    public IDictionary<string,object?>? Parameters { get; set; }
    
    /// <summary>
    /// Transport layer and codec-specific feedback messages for this codec.
    /// </summary>
    public List<RtcpFeedback>? RtcpFeedback { get; set; }
}