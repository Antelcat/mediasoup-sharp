using FBS.RtpParameters;
using FBS.Transport;
using MediasoupSharp.Constants;
using MediasoupSharp.Settings;

namespace MediasoupSharp;

public class MediasoupOptions
{
    public MediasoupStartupSettings MediasoupStartupSettings { get; set; }

    public MediasoupSettings MediasoupSettings { get; set; }

    public static MediasoupOptions Default { get; } = new()
    {
        MediasoupStartupSettings = new()
        {
            WorkerPath       = "mediasoup-worker",
            MediasoupVersion = "0.0.1",
            NumberOfWorkers  = Environment.ProcessorCount,
        },
        MediasoupSettings = new()
        {
            WorkerSettings = new()
            {
                LogLevel = WorkerLogLevel.Warn,
                LogTags =
                [
                    WorkerLogTag.Info,
                    WorkerLogTag.Ice,
                    WorkerLogTag.Dtls,
                    WorkerLogTag.Rtp,
                    WorkerLogTag.Srtp,
                    WorkerLogTag.Rtcp,
                    WorkerLogTag.Rtx,
                    WorkerLogTag.Bwe,
                    WorkerLogTag.Score,
                    WorkerLogTag.Simulcast,
                    WorkerLogTag.Svc,
                    WorkerLogTag.Sctp,
                    WorkerLogTag.Message
                ],
                RtcMinPort = 10000,
                RtcMaxPort = 59999,
            },
            RouterSettings = new()
            {
                RtpCodecCapabilities =
                [
                    new()
                    {
                        Kind      = MediaKind.AUDIO,
                        MimeType  = "audio/opus",
                        ClockRate = 48000,
                        Channels  = 2
                    },
                    new()
                    {
                        Kind      = MediaKind.VIDEO,
                        MimeType  = "video/VP8",
                        ClockRate = 90000,
                        Parameters = new Dictionary<string, object>
                        {
                            { "x-google-start-bitrate", 1000 }
                        }
                    },
                    new()
                    {
                        Kind      = MediaKind.VIDEO,
                        MimeType  = "video/VP9",
                        ClockRate = 90000,
                        Parameters = new Dictionary<string, object>
                        {
                            { "profile-id", 2 },
                            { "x-google-start-bitrate", 1000 }
                        }
                    },
                    new()
                    {
                        Kind      = MediaKind.VIDEO,
                        MimeType  = "video/h264",
                        ClockRate = 90000,
                        Parameters = new Dictionary<string, object>
                        {
                            { "packetization-mode", 1 },
                            { "profile-level-id", "4d0032" },
                            { "level-asymmetry-allowed", 1 },
                            { "x-google-start-bitrate", 1000 }
                        }
                    },
                    new()
                    {
                        Kind      = MediaKind.VIDEO,
                        MimeType  = "video/h264",
                        ClockRate = 90000,
                        Parameters = new Dictionary<string, object>
                        {
                            { "packetization-mode", 1 },
                            { "profile-level-id", "42e01f" },
                            { "level-asymmetry-allowed", 1 },
                            { "x-google-start-bitrate", 1000 }
                        }
                    }
                ],
            },
            WebRtcServerSettings = new()
            {
                ListenInfos =
                [
                    new()
                    {
                        Protocol         = Protocol.UDP,
                        Ip               = "0.0.0.0",
                        AnnouncedAddress = null,
                        Port             = 44444,
                        Flags            = new(),
                        PortRange        = new()
                    },
                    new()
                    {
                        Protocol         = Protocol.TCP,
                        Ip               = "0.0.0.0",
                        AnnouncedAddress = null,
                        Port             = 44444,
                        Flags            = new(),
                        PortRange        = new()
                    }
                ]
            },
            WebRtcTransportSettings = new()
            {
                ListenInfos =
                [
                    new()
                    {
                        Ip               = "0.0.0.0",
                        AnnouncedAddress = null,
                        Flags            = new(),
                        PortRange        = new()
                    }
                ],
                InitialAvailableOutgoingBitrate = 1_000_000,
                MinimumAvailableOutgoingBitrate = 600_000,
                MaxSctpMessageSize              = 256 * 1024,
                MaximumIncomingBitrate          = 1_500_000,
            },
            PlainTransportSettings = new()
            {
                ListenInfo = new()
                {
                    Ip               = "0.0.0.0",
                    AnnouncedAddress = null,
                    Flags            = new(),
                    PortRange        = new()
                },
                MaxSctpMessageSize = 256 * 1024,
            }
        }
    };
}