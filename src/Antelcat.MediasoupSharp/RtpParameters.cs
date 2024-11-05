using System.Text.Json.Serialization;
using Antelcat.MediasoupSharp.Internals.Extensions;
using FBS.RtpParameters;

namespace Antelcat.MediasoupSharp;

/// <summary>
/// The RTP capabilities define what mediasoup or an endpoint can receive at
/// media level.
/// </summary>
[Serializable]
public class RtpCapabilities
{
    /// <summary>
    /// Supported media and RTX codecs.
    /// </summary>
    public List<RtpCodecCapability>? Codecs { get; set; }

    /// <summary>
    /// Supported RTP header extensions.
    /// </summary>
    public RtpHeaderExtension[]? HeaderExtensions { get; set; }

    /// <summary>
    /// Supported Rtp capabilitie.
    /// </summary>
    public static RtpCapabilities SupportedRtpCapabilities { get; }

    static RtpCapabilities()
    {
        SupportedRtpCapabilities = new RtpCapabilities
        {
            Codecs =
            [
                new()
                {
                    Kind      = MediaKind.AUDIO,
                    MimeType  = "audio/opus",
                    ClockRate = 48000,
                    Channels  = 2,
                    RtcpFeedback =
                    [
                        new()
                        {
                            Type = "nacc",
                        },

                        new()
                        {
                            Type = "transport-cc",
                        }
                    ]
                },

                new()
                {
                    Kind      = MediaKind.AUDIO,
                    MimeType  = "audio/multiopus",
                    ClockRate = 48000,
                    Channels  = 4,
                    // Quad channel
                    Parameters = new()
                    {
                        { "channel_mapping", "0,1,2,3" },
                        { "num_streams", 2 },
                        { "coupled_streams", 2 },
                    },
                    RtcpFeedback =
                    [
                        new()
                        {
                            Type = "nacc",
                        },

                        new()
                        {
                            Type = "transport-cc",
                        }
                    ]
                },

                new()
                {
                    Kind      = MediaKind.AUDIO,
                    MimeType  = "audio/multiopus",
                    ClockRate = 48000,
                    Channels  = 6,
                    // 5.1
                    Parameters = new()
                    {
                        { "channel_mapping", "0,4,1,2,3,5" },
                        { "num_streams", 4 },
                        { "coupled_streams", 2 },
                    },
                    RtcpFeedback =
                    [
                        new()
                        {
                            Type = "nacc",
                        },

                        new()
                        {
                            Type = "transport-cc",
                        }
                    ]
                },

                new()
                {
                    Kind      = MediaKind.AUDIO,
                    MimeType  = "audio/multiopus",
                    ClockRate = 48000,
                    Channels  = 8,
                    // 7.1
                    Parameters = new()
                    {
                        { "channel_mapping", "0,6,1,2,3,4,5,7" },
                        { "num_streams", 5 },
                        { "coupled_streams", 4 },
                    },
                    RtcpFeedback =
                    [
                        new()
                        {
                            Type = "nacc",
                        },

                        new()
                        {
                            Type = "transport-cc",
                        }
                    ]
                },

                new()
                {
                    Kind                 = MediaKind.AUDIO,
                    MimeType             = "audio/PCMU",
                    PreferredPayloadType = 0,
                    ClockRate            = 8000,
                    RtcpFeedback =
                    [
                        new()
                        {
                            Type = "transport-cc",
                        }
                    ]
                },

                new()
                {
                    Kind                 = MediaKind.AUDIO,
                    MimeType             = "audio/PCMA",
                    PreferredPayloadType = 8,
                    ClockRate            = 8000,
                    RtcpFeedback =
                    [
                        new()
                        {
                            Type = "transport-cc",
                        }
                    ]
                },

                new()
                {
                    Kind      = MediaKind.AUDIO,
                    MimeType  = "audio/ISAC",
                    ClockRate = 32000,
                    RtcpFeedback =
                    [
                        new()
                        {
                            Type = "transport-cc",
                        }
                    ]
                },

                new()
                {
                    Kind      = MediaKind.AUDIO,
                    MimeType  = "audio/ISAC",
                    ClockRate = 16000,
                    RtcpFeedback =
                    [
                        new()
                        {
                            Type = "transport-cc",
                        }
                    ]
                },

                new()
                {
                    Kind                 = MediaKind.AUDIO,
                    MimeType             = "audio/G722",
                    PreferredPayloadType = 9,
                    ClockRate            = 8000,
                    RtcpFeedback =
                    [
                        new()
                        {
                            Type = "transport-cc",
                        }
                    ]
                },

                new()
                {
                    Kind      = MediaKind.AUDIO,
                    MimeType  = "audio/iLBC",
                    ClockRate = 8000,
                    RtcpFeedback =
                    [
                        new()
                        {
                            Type = "transport-cc",
                        }
                    ]
                },

                new()
                {
                    Kind      = MediaKind.AUDIO,
                    MimeType  = "audio/SILK",
                    ClockRate = 24000,
                    RtcpFeedback =
                    [
                        new()
                        {
                            Type = "transport-cc",
                        }
                    ]
                },

                new()
                {
                    Kind      = MediaKind.AUDIO,
                    MimeType  = "audio/SILK",
                    ClockRate = 16000,
                    RtcpFeedback =
                    [
                        new()
                        {
                            Type = "transport-cc",
                        }
                    ]
                },

                new()
                {
                    Kind      = MediaKind.AUDIO,
                    MimeType  = "audio/SILK",
                    ClockRate = 12000,
                    RtcpFeedback =
                    [
                        new()
                        {
                            Type = "transport-cc",
                        }
                    ]
                },

                new()
                {
                    Kind      = MediaKind.AUDIO,
                    MimeType  = "audio/SILK",
                    ClockRate = 8000,
                    RtcpFeedback =
                    [
                        new()
                        {
                            Type = "transport-cc",
                        }
                    ]
                },

                new()
                {
                    Kind                 = MediaKind.AUDIO,
                    MimeType             = "audio/CN",
                    PreferredPayloadType = 13,
                    ClockRate            = 32000
                },

                new()
                {
                    Kind                 = MediaKind.AUDIO,
                    MimeType             = "audio/CN",
                    PreferredPayloadType = 13,
                    ClockRate            = 16000
                },

                new()
                {
                    Kind                 = MediaKind.AUDIO,
                    MimeType             = "audio/CN",
                    PreferredPayloadType = 13,
                    ClockRate            = 8000
                },

                new()
                {
                    Kind      = MediaKind.AUDIO,
                    MimeType  = "audio/telephone-event",
                    ClockRate = 48000
                },

                new()
                {
                    Kind      = MediaKind.AUDIO,
                    MimeType  = "audio/telephone-event",
                    ClockRate = 32000
                },

                new()
                {
                    Kind      = MediaKind.AUDIO,
                    MimeType  = "audio/telephone-event",
                    ClockRate = 16000
                },

                new()
                {
                    Kind      = MediaKind.AUDIO,
                    MimeType  = "audio/telephone-event",
                    ClockRate = 8000
                },

                new()
                {
                    Kind      = MediaKind.VIDEO,
                    MimeType  = "video/VP8",
                    ClockRate = 90000,
                    RtcpFeedback =
                    [
                        new()
                        {
                            Type = "nack",
                        },

                        new()
                        {
                            Type = "nack", Parameter = "pli",
                        },

                        new()
                        {
                            Type = "ccm", Parameter = "fir",
                        },

                        new()
                        {
                            Type = "goog-remb",
                        },

                        new()
                        {
                            Type = "transport-cc",
                        }
                    ]
                },

                new()
                {
                    Kind      = MediaKind.VIDEO,
                    MimeType  = "video/VP9",
                    ClockRate = 90000,
                    RtcpFeedback =
                    [
                        new()
                        {
                            Type = "nack",
                        },

                        new()
                        {
                            Type = "nack", Parameter = "pli",
                        },

                        new()
                        {
                            Type = "ccm", Parameter = "fir",
                        },

                        new()
                        {
                            Type = "goog-remb",
                        },

                        new()
                        {
                            Type = "transport-cc",
                        }
                    ]
                },

                new()
                {
                    Kind      = MediaKind.VIDEO,
                    MimeType  = "video/H264",
                    ClockRate = 90000,
                    Parameters = new()
                    {
                        { "level-asymmetry-allowed", 1 },
                    },
                    RtcpFeedback =
                    [
                        new()
                        {
                            Type = "nack",
                        },

                        new()
                        {
                            Type = "nack", Parameter = "pli",
                        },

                        new()
                        {
                            Type = "ccm", Parameter = "fir",
                        },

                        new()
                        {
                            Type = "goog-remb",
                        },

                        new()
                        {
                            Type = "transport-cc",
                        }
                    ]
                },

                new()
                {
                    Kind      = MediaKind.VIDEO,
                    MimeType  = "video/H264-SVC",
                    ClockRate = 90000,
                    Parameters = new()
                    {
                        { "level-asymmetry-allowed", 1 },
                    },
                    RtcpFeedback =
                    [
                        new()
                        {
                            Type = "nack",
                        },

                        new()
                        {
                            Type = "nack", Parameter = "pli",
                        },

                        new()
                        {
                            Type = "ccm", Parameter = "fir",
                        },

                        new()
                        {
                            Type = "goog-remb",
                        },

                        new()
                        {
                            Type = "transport-cc",
                        }
                    ]
                },

                new()
                {
                    Kind      = MediaKind.VIDEO,
                    MimeType  = "video/H265",
                    ClockRate = 90000,
                    Parameters = new()
                    {
                        { "level-asymmetry-allowed", 1 },
                    },
                    RtcpFeedback =
                    [
                        new()
                        {
                            Type = "nack",
                        },

                        new()
                        {
                            Type = "nack", Parameter = "pli",
                        },

                        new()
                        {
                            Type = "ccm", Parameter = "fir",
                        },

                        new()
                        {
                            Type = "goog-remb",
                        },

                        new()
                        {
                            Type = "transport-cc",
                        }
                    ]
                }
            ],
            HeaderExtensions =
            [
                new()
                {
                    Kind             = MediaKind.AUDIO,
                    Uri              = RtpHeaderExtensionUri.Mid,
                    PreferredId      = 1,
                    PreferredEncrypt = false,
                    Direction        = RtpHeaderExtensionDirection.SendReceive
                },
                new()
                {
                    Kind             = MediaKind.VIDEO,
                    Uri              = RtpHeaderExtensionUri.Mid,
                    PreferredId      = 1,
                    PreferredEncrypt = false,
                    Direction        = RtpHeaderExtensionDirection.SendReceive
                },
                new()
                {
                    Kind             = MediaKind.VIDEO,
                    Uri              = RtpHeaderExtensionUri.RtpStreamId,
                    PreferredId      = 2,
                    PreferredEncrypt = false,
                    Direction        = RtpHeaderExtensionDirection.ReceiveOnly
                },
                new()
                {
                    Kind             = MediaKind.VIDEO,
                    Uri              = RtpHeaderExtensionUri.RepairRtpStreamId,
                    PreferredId      = 3,
                    PreferredEncrypt = false,
                    Direction        = RtpHeaderExtensionDirection.ReceiveOnly
                },
                new()
                {
                    Kind             = MediaKind.AUDIO,
                    Uri              = RtpHeaderExtensionUri.AbsSendTime,
                    PreferredId      = 4,
                    PreferredEncrypt = false,
                    Direction        = RtpHeaderExtensionDirection.SendReceive
                },
                new()
                {
                    Kind             = MediaKind.VIDEO,
                    Uri              = RtpHeaderExtensionUri.AbsSendTime,
                    PreferredId      = 4,
                    PreferredEncrypt = false,
                    Direction        = RtpHeaderExtensionDirection.SendReceive
                },
                // NOTE: For audio we just enable transport-wide-cc-01 when receiving media.
                new()
                {
                    Kind             = MediaKind.AUDIO,
                    Uri              = RtpHeaderExtensionUri.TransportWideCcDraft01,
                    PreferredId      = 5,
                    PreferredEncrypt = false,
                    Direction        = RtpHeaderExtensionDirection.ReceiveOnly,
                },
                new()
                {
                    Kind             = MediaKind.VIDEO,
                    Uri              = RtpHeaderExtensionUri.TransportWideCcDraft01,
                    PreferredId      = 5,
                    PreferredEncrypt = false,
                    Direction        = RtpHeaderExtensionDirection.SendReceive
                },
                // NOTE: Remove this once framemarking draft becomes RFC.
                new()
                {
                    Kind             = MediaKind.VIDEO,
                    Uri              = RtpHeaderExtensionUri.FrameMarkingDraft07,
                    PreferredId      = 6,
                    PreferredEncrypt = false,
                    Direction        = RtpHeaderExtensionDirection.SendReceive
                },
                new()
                {
                    Kind             = MediaKind.VIDEO,
                    Uri              = RtpHeaderExtensionUri.FrameMarking,
                    PreferredId      = 7,
                    PreferredEncrypt = false,
                    Direction        = RtpHeaderExtensionDirection.SendReceive
                },
                new()
                {
                    Kind             = MediaKind.AUDIO,
                    Uri              = RtpHeaderExtensionUri.AudioLevel,
                    PreferredId      = 10,
                    PreferredEncrypt = false,
                    Direction        = RtpHeaderExtensionDirection.SendReceive
                },
                new()
                {
                    Kind             = MediaKind.VIDEO,
                    Uri              = RtpHeaderExtensionUri.VideoOrientation,
                    PreferredId      = 11,
                    PreferredEncrypt = false,
                    Direction        = RtpHeaderExtensionDirection.SendReceive
                },
                new()
                {
                    Kind             = MediaKind.VIDEO,
                    Uri              = RtpHeaderExtensionUri.TimeOffset,
                    PreferredId      = 12,
                    PreferredEncrypt = false,
                    Direction        = RtpHeaderExtensionDirection.SendReceive
                },
                new()
                {
                    Kind             = MediaKind.AUDIO,
                    Uri              = RtpHeaderExtensionUri.AbsCaptureTime,
                    PreferredId      = 13,
                    PreferredEncrypt = false,
                    Direction        = RtpHeaderExtensionDirection.SendReceive
                },
                new()
                {
                    Kind             = MediaKind.VIDEO,
                    Uri              = RtpHeaderExtensionUri.AbsCaptureTime,
                    PreferredId      = 13,
                    PreferredEncrypt = false,
                    Direction        = RtpHeaderExtensionDirection.SendReceive
                },
                new()
                {
                    Kind             = MediaKind.AUDIO,
                    Uri              = RtpHeaderExtensionUri.PlayoutDelay,
                    PreferredId      = 14,
                    PreferredEncrypt = false,
                    Direction        = RtpHeaderExtensionDirection.SendReceive,
                },
                new()
                {
                    Kind             = MediaKind.VIDEO,
                    Uri              = RtpHeaderExtensionUri.PlayoutDelay,
                    PreferredId      = 14,
                    PreferredEncrypt = false,
                    Direction        = RtpHeaderExtensionDirection.SendReceive,
                }
            ]
        };
    }
}

[Serializable]
public class RtpCodecShared
{
    /// <summary>
    /// The codec MIME media type/subtype (e.g. 'audio/opus', 'video/VP8').
    /// </summary>
    public required string MimeType { get; set; }

    /// <summary>
    /// Codec clock rate expressed in Hertz.
    /// </summary>
    public uint ClockRate { get; set; }

    /// <summary>
    /// The number of channels supported (e.g. two for stereo). Just for audio.
    /// Default 1.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public byte? Channels { get; set; }

    /// <summary>
    /// Codec-specific parameters available for signaling. Some parameters (such
    /// as 'packetization-mode' and 'profile-level-id' in H264 or 'profile-id' in
    /// VP9) are critical for codec matching.
    /// </summary>
    public Dictionary<string, object?>? Parameters { get; set; }
}

/// <summary>
/// <para>
/// Provides information on the capabilities of a codec within the RTP
/// capabilities. The list of media codecs supported by mediasoup and their
/// settings is defined in the supportedRtpCapabilities.ts file.
/// </para>
/// <para>
/// Exactly one RtpCodecCapability will be present for each supported combination
/// of parameters that requires a distinct value of preferredPayloadType. For
/// example:
/// </para>
/// <para>
/// - Multiple H264 codecs, each with their own distinct 'packetization-mode' and
/// 'profile-level-id' values.
/// - Multiple VP9 codecs, each with their own distinct 'profile-id' value.
/// </para>
/// <para>
/// RtpCodecCapability entries in the mediaCodecs array of RouterOptions do not
/// require preferredPayloadType field (if unset, mediasoup will choose a random
/// one). If given, make sure it's in the 96-127 range.
/// </para>
/// </summary>
[Serializable]
public class RtpCodecCapability : RtpCodecShared
{
    /// <summary>
    /// Media kind.
    /// </summary>
    public MediaKind Kind { get; set; }

    /// <summary>
    /// The preferred RTP payload type.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public byte? PreferredPayloadType { get; set; }

    /// <summary>
    /// Transport layer and codec-specific feedback messages for this codec.
    /// </summary>
    public List<RtcpFeedbackT>? RtcpFeedback { get; set; }
}

/// <summary>
/// Direction of RTP header extension.
/// </summary>
public enum RtpHeaderExtensionDirection
{
    SendReceive,
    SendOnly,
    ReceiveOnly,
    Inactive
}

/// <summary>
/// <para>
/// Provides information relating to supported header extensions. The list of
/// RTP header extensions supported by mediasoup is defined in the
/// supportedRtpCapabilities.ts file.
/// </para>
/// <para>
/// mediasoup does not currently support encrypted RTP header extensions. The
/// direction field is just present in mediasoup RTP capabilities (retrieved via
/// router.rtpCapabilities or mediasoup.getSupportedRtpCapabilities()). It's
/// ignored if present in endpoints' RTP capabilities.
/// </para>
/// </summary>
[Serializable]
public class RtpHeaderExtension
{
    /// <summary>
    /// Media kind.
    /// Default any media kind.
    /// </summary>
    public MediaKind Kind { get; set; }

    /// <summary>
    /// The URI of the RTP header extension, as defined in RFC 5285.
    /// </summary>
    public RtpHeaderExtensionUri Uri { get; set; }

    /// <summary>
    /// The preferred numeric identifier that goes in the RTP packet. Must be
    /// unique.
    /// </summary>
    public byte PreferredId { get; set; }

    /// <summary>
    /// If true, it is preferred that the value in the header be encrypted as per
    /// RFC 6904. Default false.
    /// </summary>
    public bool PreferredEncrypt { get; set; }

    /// <summary>
    /// If 'sendrecv', mediasoup supports sending and receiving this RTP extension.
    /// 'sendonly' means that mediasoup can send (but not receive) it. 'recvonly'
    /// means that mediasoup can receive (but not send) it.
    /// </summary>
    public RtpHeaderExtensionDirection? Direction { get; set; }
}

/// <summary>
/// <para>
/// The RTP send parameters describe a media stream received by mediasoup from
/// an endpoint through its corresponding mediasoup Producer. These parameters
/// may include a mid value that the mediasoup transport will use to match
/// received RTP packets based on their MID RTP extension value.
/// </para>
/// <para>
/// mediasoup allows RTP send parameters with a single encoding and with multiple
/// encodings (simulcast). In the latter case, each entry in the encodings array
/// must include a ssrc field or a rid field (the RID RTP extension value). Check
/// the Simulcast and SVC sections for more information.
/// </para>
/// <para>
/// The RTP receive parameters describe a media stream as sent by mediasoup to
/// an endpoint through its corresponding mediasoup Consumer. The mid value is
/// unset (mediasoup does not include the MID RTP extension into RTP packets
/// being sent to endpoints).
/// </para>
/// <para>
/// There is a single entry in the encodings array (even if the corresponding
/// producer uses simulcast). The consumer sends a single and continuous RTP
/// stream to the endpoint and spatial/temporal layer selection is possible via
/// consumer.setPreferredLayers().
/// </para>
/// <para>
/// As an exception, previous bullet is not true when consuming a stream over a
/// PipeTransport, in which all RTP streams from the associated producer are
/// forwarded verbatim through the consumer.
/// </para>
/// <para>
/// The RTP receive parameters will always have their ssrc values randomly
/// generated for all of its  encodings (and optional rtx: { ssrc: XXXX } if the
/// endpoint supports RTX), regardless of the original RTP send parameters in
/// the associated producer. This applies even if the producer's encodings have
/// rid set.
/// </para>
/// </summary>
[Serializable]
public class RtpParameters
{
    /// <summary>
    /// The MID RTP extension value as defined in the BUNDLE specification.
    /// </summary>
    public string? Mid { get; set; }

    /// <summary>
    /// Media and RTX codecs in use.
    /// </summary>
    public List<RtpCodecParameters> Codecs { get; set; }

    /// <summary>
    /// RTP header extensions in use.
    /// </summary>
    public List<RtpHeaderExtensionParameters>? HeaderExtensions { get; set; }

    /// <summary>
    /// Transmitted RTP streams and their settings.
    /// </summary>
    public List<RtpEncodingParametersT> Encodings { get; set; } = [];

    /// <summary>
    /// Parameters used for RTCP.
    /// </summary>
    public RtcpParametersT Rtcp { get; set; } = new();
}

/// <summary>
/// Provides information on codec settings within the RTP parameters. The list
/// of media codecs supported by mediasoup and their settings is defined in the
/// supportedRtpCapabilities.ts file.
/// </summary>
[Serializable]
public class RtpCodecParameters : RtpCodecShared, IEquatable<RtpCodecParameters>
{
    /// <summary>
    /// The value that goes in the RTP Payload Type Field. Must be unique.
    /// </summary>
    public byte PayloadType { get; set; }

    /// <summary>
    /// Transport layer and codec-specific feedback messages for this codec.
    /// </summary>
    public List<RtcpFeedbackT> RtcpFeedback { get; set; } = [];

    public bool Equals(RtpCodecParameters? other)
    {
        if (other == null)
        {
            return false;
        }

        var result = MimeType       == other.MimeType
                     && PayloadType == other.PayloadType
                     && ClockRate   == other.ClockRate;
        if (result)
        {
            if (Channels.HasValue && other.Channels.HasValue)
            {
                result = Channels == other.Channels;
            }
            else if (Channels.HasValue ^ other.Channels.HasValue)
            {
                result = false;
            }
        }

        if (result)
        {
            if (Parameters != null && other.Parameters != null)
            {
                result = Parameters.DeepEquals(other.Parameters);
            }
            else if ((Parameters == null && other.Parameters != null) ||
                     (Parameters != null && other.Parameters == null))
            {
                result = false;
            }
        }

        return result;
    }

    public override bool Equals(object? other)
    {
        if (other is RtpCodecParameters rtpCodecParameters)
        {
            return Equals(rtpCodecParameters);
        }

        return false;
    }

    public override int GetHashCode()
    {
        var result = MimeType.GetHashCode() ^ PayloadType.GetHashCode() ^ ClockRate.GetHashCode();
        if (Parameters != null)
        {
            result = Parameters.DeepGetHashCode() ^ result;
        }

        return result;
    }
}

/// <summary>
/// <para>
/// Defines a RTP header extension within the RTP parameters. The list of RTP
/// header extensions supported by mediasoup is defined in the
/// supportedRtpCapabilities.ts file.
/// </para>
/// <para>
/// mediasoup does not currently support encrypted RTP header extensions and no
/// parameters are currently considered.
/// </para>
/// </summary>
[Serializable]
public class RtpHeaderExtensionParameters
{
    /// <summary>
    /// The URI of the RTP header extension, as defined in RFC 5285.
    /// </summary>
    public RtpHeaderExtensionUri Uri { get; set; }

    /// <summary>
    /// The numeric identifier that goes in the RTP packet. Must be unique.
    /// </summary>
    public byte Id { get; set; }

    /// <summary>
    /// If true, the value in the header is encrypted as per RFC 6904. Default false.
    /// </summary>
    public bool Encrypt { get; set; }

    /// <summary>
    /// Configuration parameters for the header extension.
    /// </summary>
    public Dictionary<string, object?>? Parameters { get; set; }
}

/// <summary>
/// <para>Provides information on RTCP settings within the RTP parameters.</para>
/// <para>
/// If no cname is given in a producer's RTP parameters, the mediasoup transport
/// will choose a random one that will be used into RTCP SDES messages sent to
/// all its associated consumers.
/// </para>
/// <para>mediasoup assumes reducedSize to always be true.</para>
/// </summary>
[Serializable]
public class RtcpParameters
{
    /// <summary>
    /// The Canonical Name (CNAME) used by RTCP (e.g. in SDES messages).
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Cname { get; set; }

    /// <summary>
    /// Whether reduced size RTCP RFC 5506 is configured (if true) or compound RTCP
    /// as specified in RFC 3550 (if false). Default true.
    /// </summary>
    public bool? ReducedSize { get; set; } = true;
}