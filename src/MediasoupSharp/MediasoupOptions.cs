using System.Reflection;
using FlatBuffers.RtpParameters;
using FlatBuffers.Transport;
using MediasoupSharp.Constants;
using MediasoupSharp.FlatBuffers.Transport.T;
using MediasoupSharp.RtpParameters;
using MediasoupSharp.Settings;

namespace MediasoupSharp;

public class MediasoupOptions
{
    public MediasoupStartupSettings MediasoupStartupSettings { get; set; }

    public MediasoupSettings MediasoupSettings { get; set; }

    public static MediasoupOptions Default { get; } = new()
    {
        MediasoupStartupSettings = new MediasoupStartupSettings
        {
            WorkerPath       = "mediasoup-worker",
            MediasoupVersion = typeof(MediasoupOptions).Assembly.ImageRuntimeVersion,
            NumberOfWorkers  = Environment.ProcessorCount,
        },
        MediasoupSettings = new MediasoupSettings
        {
            WorkerSettings = new WorkerSettings
            {
                LogLevel = WorkerLogLevel.warn,
                LogTags =
                [
                    WorkerLogTag.info,
                    WorkerLogTag.ice,
                    WorkerLogTag.dtls,
                    WorkerLogTag.rtp,
                    WorkerLogTag.srtp,
                    WorkerLogTag.rtcp,
                    WorkerLogTag.rtx,
                    WorkerLogTag.bwe,
                    WorkerLogTag.score,
                    WorkerLogTag.simulcast,
                    WorkerLogTag.svc,
                    WorkerLogTag.sctp,
                    WorkerLogTag.message
                ],
                RtcMinPort = 10000,
                RtcMaxPort = 59999,
            },
            RouterSettings = new RouterSettings
            {
                RtpCodecCapabilities =
                [
                    new RtpCodecCapability
                    {
                        Kind      = MediaKind.audio,
                        MimeType  = "audio/opus",
                        ClockRate = 48000,
                        Channels  = 2
                    },
                    new RtpCodecCapability
                    {
                        Kind      = MediaKind.video,
                        MimeType  = "video/VP8",
                        ClockRate = 90000,
                        Parameters = new Dictionary<string, object>
                        {
                            { "x-google-start-bitrate", 1000 }
                        }
                    },
                    new RtpCodecCapability
                    {
                        Kind      = MediaKind.video,
                        MimeType  = "video/VP9",
                        ClockRate = 90000,
                        Parameters = new Dictionary<string, object>
                        {
                            { "profile-id", 2 },
                            { "x-google-start-bitrate", 1000 }
                        }
                    },
                    new RtpCodecCapability
                    {
                        Kind      = MediaKind.video,
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
                    new RtpCodecCapability
                    {
                        Kind      = MediaKind.video,
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
            WebRtcServerSettings = new WebRtcServerSettings
            {
                ListenInfos =
                [
                    new ListenInfoT
                    {
                        Protocol    = Protocol.UDP,
                        Ip          = "0.0.0.0",
                        AnnouncedIp = null,
                        Port        = 44444,
                    },
                    new ListenInfoT
                    {
                        Protocol    = Protocol.TCP,
                        Ip          = "0.0.0.0",
                        AnnouncedIp = null,
                        Port        = 44444,
                    }
                ]
            },
            WebRtcTransportSettings = new WebRtcTransportSettings
            {
                ListenInfos =
                [
                    new ListenInfoT { Ip = "0.0.0.0", AnnouncedIp = null }
                ],
                InitialAvailableOutgoingBitrate = 1_000_000,
                MinimumAvailableOutgoingBitrate = 600_000,
                MaxSctpMessageSize              = 256 * 1024,
                MaximumIncomingBitrate          = 1_500_000,
            },
            PlainTransportSettings = new PlainTransportSettings
            {
                ListenInfo         = new ListenInfoT { Ip = "0.0.0.0", AnnouncedIp = null },
                MaxSctpMessageSize = 256 * 1024,
            }
        }
    };
}