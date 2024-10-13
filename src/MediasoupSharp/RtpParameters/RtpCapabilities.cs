using FlatBuffers.RtpParameters;
using MediasoupSharp.FlatBuffers.RtpParameters.T;

namespace MediasoupSharp.RtpParameters;

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
                    Kind      = MediaKind.audio,
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
                    Kind      = MediaKind.audio,
                    MimeType  = "audio/multiopus",
                    ClockRate = 48000,
                    Channels  = 4,
                    // Quad channel
                    Parameters = new Dictionary<string, object>
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
                    Kind      = MediaKind.audio,
                    MimeType  = "audio/multiopus",
                    ClockRate = 48000,
                    Channels  = 6,
                    // 5.1
                    Parameters = new Dictionary<string, object>
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
                    Kind      = MediaKind.audio,
                    MimeType  = "audio/multiopus",
                    ClockRate = 48000,
                    Channels  = 8,
                    // 7.1
                    Parameters = new Dictionary<string, object>
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
                    Kind                 = MediaKind.audio,
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
                    Kind                 = MediaKind.audio,
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
                    Kind      = MediaKind.audio,
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
                    Kind      = MediaKind.audio,
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
                    Kind                 = MediaKind.audio,
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
                    Kind      = MediaKind.audio,
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
                    Kind      = MediaKind.audio,
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
                    Kind      = MediaKind.audio,
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
                    Kind      = MediaKind.audio,
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
                    Kind      = MediaKind.audio,
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
                    Kind                 = MediaKind.audio,
                    MimeType             = "audio/CN",
                    PreferredPayloadType = 13,
                    ClockRate            = 32000
                },

                new()
                {
                    Kind                 = MediaKind.audio,
                    MimeType             = "audio/CN",
                    PreferredPayloadType = 13,
                    ClockRate            = 16000
                },

                new()
                {
                    Kind                 = MediaKind.audio,
                    MimeType             = "audio/CN",
                    PreferredPayloadType = 13,
                    ClockRate            = 8000
                },

                new()
                {
                    Kind      = MediaKind.audio,
                    MimeType  = "audio/telephone-event",
                    ClockRate = 48000
                },

                new()
                {
                    Kind      = MediaKind.audio,
                    MimeType  = "audio/telephone-event",
                    ClockRate = 32000
                },

                new()
                {
                    Kind      = MediaKind.audio,
                    MimeType  = "audio/telephone-event",
                    ClockRate = 16000
                },

                new()
                {
                    Kind      = MediaKind.audio,
                    MimeType  = "audio/telephone-event",
                    ClockRate = 8000
                },

                new()
                {
                    Kind      = MediaKind.video,
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
                    Kind      = MediaKind.video,
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
                    Kind      = MediaKind.video,
                    MimeType  = "video/H264",
                    ClockRate = 90000,
                    Parameters = new Dictionary<string, object>
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
                    Kind      = MediaKind.video,
                    MimeType  = "video/H264-SVC",
                    ClockRate = 90000,
                    Parameters = new Dictionary<string, object>
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
                    Kind      = MediaKind.video,
                    MimeType  = "video/H265",
                    ClockRate = 90000,
                    Parameters = new Dictionary<string, object>
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
                new () {
                    Kind             = MediaKind.audio,
                    Uri              =  RtpHeaderExtensionUri.Mid,
                    PreferredId      = 1,
                    PreferredEncrypt = false,
                    Direction        = RtpHeaderExtensionDirection.SendReceive
                },
                new () {
                    Kind             = MediaKind.video,
                    Uri              = RtpHeaderExtensionUri.Mid,
                    PreferredId      = 1,
                    PreferredEncrypt = false,
                    Direction        = RtpHeaderExtensionDirection.SendReceive
                },
                new () {
                    Kind             = MediaKind.video,
                    Uri              = RtpHeaderExtensionUri.RtpStreamId,
                    PreferredId      = 2,
                    PreferredEncrypt = false,
                    Direction        = RtpHeaderExtensionDirection.ReceiveOnly
                },
                new () {
                    Kind             = MediaKind.video,
                    Uri              = RtpHeaderExtensionUri.RepairRtpStreamId,
                    PreferredId      = 3,
                    PreferredEncrypt = false,
                    Direction        = RtpHeaderExtensionDirection.ReceiveOnly
                },
                new () {
                    Kind             = MediaKind.audio,
                    Uri              = RtpHeaderExtensionUri.AbsSendTime,
                    PreferredId      = 4,
                    PreferredEncrypt = false,
                    Direction        = RtpHeaderExtensionDirection.SendReceive
                },
                new () {
                    Kind             = MediaKind.video,
                    Uri              = RtpHeaderExtensionUri.AbsSendTime,
                    PreferredId      = 4,
                    PreferredEncrypt = false,
                    Direction        = RtpHeaderExtensionDirection.SendReceive
                },
                // NOTE: For audio we just enable transport-wide-cc-01 when receiving media.
                new () {
                    Kind             = MediaKind.audio,
                    Uri              = RtpHeaderExtensionUri.TransportWideCcDraft01,
                    PreferredId      = 5,
                    PreferredEncrypt = false,
                    Direction        = RtpHeaderExtensionDirection.ReceiveOnly,
                },
                new () {
                    Kind             = MediaKind.video,
                    Uri              = RtpHeaderExtensionUri.TransportWideCcDraft01,
                    PreferredId      = 5,
                    PreferredEncrypt = false,
                    Direction        = RtpHeaderExtensionDirection.SendReceive
                },
                // NOTE: Remove this once framemarking draft becomes RFC.
                new () {
                    Kind             = MediaKind.video,
                    Uri              = RtpHeaderExtensionUri.FrameMarkingDraft07,
                    PreferredId      = 6,
                    PreferredEncrypt = false,
                    Direction        = RtpHeaderExtensionDirection.SendReceive
                },
                new () {
                    Kind             = MediaKind.video,
                    Uri              = RtpHeaderExtensionUri.FrameMarking,
                    PreferredId      = 7,
                    PreferredEncrypt = false,
                    Direction        = RtpHeaderExtensionDirection.SendReceive
                },
                new () {
                    Kind             = MediaKind.audio,
                    Uri              = RtpHeaderExtensionUri.AudioLevel,
                    PreferredId      = 10,
                    PreferredEncrypt = false,
                    Direction        = RtpHeaderExtensionDirection.SendReceive
                },
                new () {
                    Kind             = MediaKind.video,
                    Uri              = RtpHeaderExtensionUri.VideoOrientation,
                    PreferredId      = 11,
                    PreferredEncrypt = false,
                    Direction        = RtpHeaderExtensionDirection.SendReceive
                },
                new () {
                    Kind             = MediaKind.video,
                    Uri              = RtpHeaderExtensionUri.TimeOffset,
                    PreferredId      = 12,
                    PreferredEncrypt = false,
                    Direction        = RtpHeaderExtensionDirection.SendReceive
                },
                new () {
                    Kind             = MediaKind.audio,
                    Uri              = RtpHeaderExtensionUri.AbsCaptureTime,
                    PreferredId      = 13,
                    PreferredEncrypt = false,
                    Direction        = RtpHeaderExtensionDirection.SendReceive
                },
                new () {
                    Kind             = MediaKind.video,
                    Uri              = RtpHeaderExtensionUri.AbsCaptureTime,
                    PreferredId      = 13,
                    PreferredEncrypt = false,
                    Direction        = RtpHeaderExtensionDirection.SendReceive
                }
            ]
        };
    }
}