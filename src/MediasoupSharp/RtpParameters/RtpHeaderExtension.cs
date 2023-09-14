namespace MediasoupSharp.RtpParameters;

public record RtpHeaderExtension
{
   
    /// <summary>
    /// Media kind.
    /// </summary>
    public MediaKind Kind { get; set; }

    /// <summary>
    /// The URI of the RTP header extension, as defined in RFC 5285.
    /// </summary>
    public string Uri { get; set; } = string.Empty;

    /// <summary>
    /// The preferred numeric identifier that goes in the RTP packet. Must be
    /// unique.
    /// </summary>
    public int PreferredId { get; set; }

    /// <summary>
    /// If true, it is preferred that the value in the header be encrypted as per
    /// RFC 6904. Default false.
    /// </summary>
    public bool? PreferredEncrypt { get; set; }

    /// <summary>
    /// If 'sendrecv', mediasoup supports sending and receiving this RTP extension.
    /// 'sendonly' means that mediasoup can send (but not receive) it. 'recvonly'
    /// means that mediasoup can receive (but not send) it.
    /// </summary>
    public RtpHeaderExtensionDirection? Direction { get; set; }
}