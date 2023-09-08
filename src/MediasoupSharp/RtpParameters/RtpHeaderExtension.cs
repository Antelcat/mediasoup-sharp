namespace MediasoupSharp.RtpParameters;

public record RtpHeaderExtension
{
    /**
     * Media kind.
     */
    public MediaKind Kind { get; set; }

    /*
     * The URI of the RTP header extension, as defined in RFC 5285.
     */
    public string Uri { get; set; } = string.Empty;

    /**
     * The preferred numeric identifier that goes in the RTP packet. Must be
     * unique.
     */
    public int PreferredId { get; set; }

    /**
     * If true, it is preferred that the value in the header be encrypted as per
     * RFC 6904. Default false.
     */
    public bool? PreferredEncrypt { get; set; }

    /**
     * If 'sendrecv', mediasoup supports sending and receiving this RTP extension.
     * 'sendonly' means that mediasoup can send (but not receive) it. 'recvonly'
     * means that mediasoup can receive (but not send) it.
     */
    public RtpHeaderExtensionDirection? Direction { get; set; }
}