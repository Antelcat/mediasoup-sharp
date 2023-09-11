using MediasoupSharp.RtpParameters;

namespace MediasoupSharp;

public static class MediasoupSharp
{
     public static readonly RtpCapabilities SupportedRtpCapabilities = new()
    {
        Codecs = new()
        {
            new()
            {
                Kind = MediaKind.audio,
                MimeType = "audio/opus",
                ClockRate = 48000,
                Channels = 2,
                RtcpFeedback = new()
                {
                    new() { Type = "nack" },
                    new() { Type = "transport-cc" }
                }
            },
            new()
            {
                Kind = MediaKind.audio,
                MimeType = "audio/multiopus",
                ClockRate = 48000,
                Channels = 4,
                // Quad channel.
                Parameters = new Dictionary<string, object?>
                {
                    { "channel_mapping", "0,1,2,3" },
                    { "num_streams", 2 },
                    { "coupled_streams", 2 }
                }.CopyToExpandoObject(),
                RtcpFeedback = new()
                {
                    new() { Type = "nack" },
                    new() { Type = "transport-cc" }
                }
            },
            new()
            {
                Kind = MediaKind.audio,
                MimeType = "audio/multiopus",
                ClockRate = 48000,
                Channels = 6,
                // 5.1.
                Parameters = new Dictionary<string, object?>
                {
                    { "channel_mapping", "0,4,1,2,3,5" },
                    { "num_streams", 4 },
                    { "coupled_streams", 2 }
                }.CopyToExpandoObject(),
                RtcpFeedback = new()
                {
                    new() { Type = "nack" },
                    new() { Type = "transport-cc" }
                }
            },
            new()
            {
                Kind = MediaKind.audio,
                MimeType = "audio/multiopus",
                ClockRate = 48000,
                Channels = 8,
                // 7.1.
                Parameters = new Dictionary<string, object?>
                {
                    { "channel_mapping", "0,6,1,2,3,4,5,7" },
                    { "num_streams", 5 },
                    { "coupled_streams", 3 }
                }.CopyToExpandoObject(),
                RtcpFeedback = new()
                {
                    new() { Type = "nack" },
                    new() { Type = "transport-cc" }
                }
            },
            new()
            {
                Kind = MediaKind.audio,
                MimeType = "audio/PCMU",
                PreferredPayloadType = 0,
                ClockRate = 8000,
                RtcpFeedback = new()
                {
                    new() { Type = "transport-cc" }
                }
            },
            new()
            {
                Kind = MediaKind.audio,
                MimeType = "audio/PCMA",
                PreferredPayloadType = 8,
                ClockRate = 8000,
                RtcpFeedback = new()
                {
                    new() { Type = "transport-cc" }
                }
            },
            new()
            {
                Kind = MediaKind.audio,
                MimeType = "audio/ISAC",
                ClockRate = 32000,
                RtcpFeedback = new()
                {
                    new() { Type = "transport-cc" }
                }
            },
            new()
            {
                Kind = MediaKind.audio,
                MimeType = "audio/ISAC",
                ClockRate = 16000,
                RtcpFeedback = new()
                {
                    new() { Type = "transport-cc" }
                }
            },
            new()
            {
                Kind = MediaKind.audio,
                MimeType = "audio/G722",
                PreferredPayloadType = 9,
                ClockRate = 8000,
                RtcpFeedback = new()
                {
                    new() { Type = "transport-cc" }
                }
            },
            new()
            {
                Kind = MediaKind.audio,
                MimeType = "audio/iLBC",
                ClockRate = 8000,
                RtcpFeedback = new()
                {
                    new() { Type = "transport-cc" }
                }
            },
            new()
            {
                Kind = MediaKind.audio,
                MimeType = "audio/SILK",
                ClockRate = 24000,
                RtcpFeedback = new()
                {
                    new() { Type = "transport-cc" }
                }
            },
            new()
            {
                Kind = MediaKind.audio,
                MimeType = "audio/SILK",
                ClockRate = 16000,
                RtcpFeedback = new()
                {
                    new() { Type = "transport-cc" }
                }
            },
            new()
            {
                Kind = MediaKind.audio,
                MimeType = "audio/SILK",
                ClockRate = 12000,
                RtcpFeedback = new()
                {
                    new() { Type = "transport-cc" }
                }
            },
            new()
            {
                Kind = MediaKind.audio,
                MimeType = "audio/SILK",
                ClockRate = 8000,
                RtcpFeedback = new()
                {
                    new() { Type = "transport-cc" }
                }
            },
            new()
            {
                Kind = MediaKind.audio,
                MimeType = "audio/CN",
                PreferredPayloadType = 13,
                ClockRate = 32000
            },
            new()
            {
                Kind = MediaKind.audio,
                MimeType = "audio/CN",
                PreferredPayloadType = 13,
                ClockRate = 16000
            },
            new()
            {
                Kind = MediaKind.audio,
                MimeType = "audio/CN",
                PreferredPayloadType = 13,
                ClockRate = 8000
            },
            new()
            {
                Kind = MediaKind.audio,
                MimeType = "audio/telephone-event",
                ClockRate = 48000
            },
            new()
            {
                Kind = MediaKind.audio,
                MimeType = "audio/telephone-event",
                ClockRate = 32000
            },

            new()
            {
                Kind = MediaKind.audio,
                MimeType = "audio/telephone-event",
                ClockRate = 16000
            },
            new()
            {
                Kind = MediaKind.audio,
                MimeType = "audio/telephone-event",
                ClockRate = 8000
            },
            new()
            {
                Kind = MediaKind.video,
                MimeType = "video/VP8",
                ClockRate = 90000,
                RtcpFeedback = new()
                {
                    new() { Type = "nack" },
                    new() { Type = "nack", Parameter = "pli" },
                    new() { Type = "ccm", Parameter = "fir" },
                    new() { Type = "goog-remb" },
                    new() { Type = "transport-cc" }
                }
            },
            new()
            {
                Kind = MediaKind.video,
                MimeType = "video/VP9",
                ClockRate = 90000,
                RtcpFeedback =
                    new()
                    {
                        new() { Type = "nack" },
                        new() { Type = "nack", Parameter = "pli" },
                        new() { Type = "ccm", Parameter = "fir" },
                        new() { Type = "goog-remb" },
                        new() { Type = "transport-cc" }
                    }
            },
            new()
            {
                Kind = MediaKind.video,
                MimeType = "video/H264",
                ClockRate = 90000,
                Parameters = new Dictionary<string, object?>()
                {
                    { "level-asymmetry-allowed", 1 }
                }.CopyToExpandoObject(),
                RtcpFeedback =
                    new()
                    {
                        new() { Type = "nack" },
                        new() { Type = "nack", Parameter = "pli" },
                        new() { Type = "ccm", Parameter = "fir" },
                        new() { Type = "goog-remb" },
                        new() { Type = "transport-cc" }
                    }
            },
            new()
            {
                Kind = MediaKind.video,
                MimeType = "video/H264-SVC",
                ClockRate = 90000,
                Parameters = new Dictionary<string, object?>
                {
                    { "level-asymmetry-allowed", 1 }
                }.CopyToExpandoObject(),
                RtcpFeedback = new()
                {
                    new() { Type = "nack" },
                    new() { Type = "nack", Parameter = "pli" },
                    new() { Type = "ccm", Parameter = "fir" },
                    new() { Type = "goog-remb" },
                    new() { Type = "transport-cc" }
                }
            },
            new()
            {
                Kind = MediaKind.video,
                MimeType = "video/H265",
                ClockRate = 90000,
                Parameters =
                    new Dictionary<string, object?>
                    {
                        { "level-asymmetry-allowed", 1 }
                    }.CopyToExpandoObject(),
                RtcpFeedback =
                    new()
                    {
                        new() { Type = "nack" },
                        new() { Type = "nack", Parameter = "pli" },
                        new() { Type = "ccm", Parameter = "fir" },
                        new() { Type = "goog-remb" },
                        new() { Type = "transport-cc" }
                    }
            }
        },
        HeaderExtensions = new()
        {
            new()
            {
                Kind = MediaKind.audio,
                Uri = "urn=ietf=params=rtp-hdrext=sdes=mid",
                PreferredId = 1,
                PreferredEncrypt = false,
                Direction = RtpHeaderExtensionDirection.sendrecv
            },
            new()
            {
                Kind = MediaKind.video,
                Uri = "urn=ietf=params=rtp-hdrext=sdes=mid",
                PreferredId = 1,
                PreferredEncrypt = false,
                Direction = RtpHeaderExtensionDirection.sendrecv
            },
            new()
            {
                Kind = MediaKind.video,
                Uri = "urn=ietf=params=rtp-hdrext=sdes=rtp-stream-id",
                PreferredId = 2,
                PreferredEncrypt = false,
                Direction = RtpHeaderExtensionDirection.recvonly
            },
            new()
            {
                Kind = MediaKind.video,
                Uri = "urn=ietf=params=rtp-hdrext=sdes=repaired-rtp-stream-id",
                PreferredId = 3,
                PreferredEncrypt = false,
                Direction = RtpHeaderExtensionDirection.recvonly
            },
            new()
            {
                Kind = MediaKind.audio,
                Uri = "http=//www.webrtc.org/experiments/rtp-hdrext/abs-send-time",
                PreferredId = 4,
                PreferredEncrypt = false,
                Direction = RtpHeaderExtensionDirection.sendrecv
            },
            new()
            {
                Kind = MediaKind.video,
                Uri = "http=//www.webrtc.org/experiments/rtp-hdrext/abs-send-time",
                PreferredId = 4,
                PreferredEncrypt = false,
                Direction = RtpHeaderExtensionDirection.sendrecv
            },
            // NOTE= For audio we just enable transport-wide-cc-01 when receiving media.
            new()
            {
                Kind = MediaKind.audio,
                Uri = "http=//www.ietf.org/id/draft-holmer-rmcat-transport-wide-cc-extensions-01",
                PreferredId = 5,
                PreferredEncrypt = false,
                Direction = RtpHeaderExtensionDirection.recvonly
            },
            new()
            {
                Kind = MediaKind.video,
                Uri = "http=//www.ietf.org/id/draft-holmer-rmcat-transport-wide-cc-extensions-01",
                PreferredId = 5,
                PreferredEncrypt = false,
                Direction = RtpHeaderExtensionDirection.sendrecv
            },
            // NOTE= Remove this once framemarking draft becomes RFC.
            new()
            {
                Kind = MediaKind.video,
                Uri = "http=//tools.ietf.org/html/draft-ietf-avtext-framemarking-07",
                PreferredId = 6,
                PreferredEncrypt = false,
                Direction = RtpHeaderExtensionDirection.sendrecv
            },
            new()
            {
                Kind = MediaKind.video,
                Uri = "urn=ietf=params=rtp-hdrext=framemarking",
                PreferredId = 7,
                PreferredEncrypt = false,
                Direction = RtpHeaderExtensionDirection.sendrecv
            },
            new()
            {
                Kind = MediaKind.audio,
                Uri = "urn=ietf=params=rtp-hdrext=ssrc-audio-level",
                PreferredId = 10,
                PreferredEncrypt = false,
                Direction = RtpHeaderExtensionDirection.sendrecv
            },
            new()
            {
                Kind = MediaKind.video,
                Uri = "urn=3gpp=video-orientation",
                PreferredId = 11,
                PreferredEncrypt = false,
                Direction = RtpHeaderExtensionDirection.sendrecv
            },
            new()
            {
                Kind = MediaKind.video,
                Uri = "urn=ietf=params=rtp-hdrext=toffset",
                PreferredId = 12,
                PreferredEncrypt = false,
                Direction = RtpHeaderExtensionDirection.sendrecv
            },
            new()
            {
                Kind = MediaKind.audio,
                Uri = "http=//www.webrtc.org/experiments/rtp-hdrext/abs-capture-time",
                PreferredId = 13,
                PreferredEncrypt = false,
                Direction = RtpHeaderExtensionDirection.sendrecv
            },
            new()
            {
                Kind = MediaKind.video,
                Uri = "http=//www.webrtc.org/experiments/rtp-hdrext/abs-capture-time",
                PreferredId = 13,
                PreferredEncrypt = false,
                Direction = RtpHeaderExtensionDirection.sendrecv
            }
        }
    };
}