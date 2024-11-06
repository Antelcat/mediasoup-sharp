using FBS.RtpParameters;
using FBS.Transport;

namespace Antelcat.MediasoupSharp.AspNetCore;

public class MediasoupOptionsContext<T>
{
    public int?                       NumWorkers             { get; set; } = Environment.ProcessorCount;
    public WorkerSettings<T>?         WorkerSettings         { get; init; }
    public RouterOptions<T>?          RouterOptions          { get; init; }
    public WebRtcServerOptions<T>?    WebRtcServerOptions    { get; init; }
    public WebRtcTransportOptions<T>? WebRtcTransportOptions { get; init; }
    public PlainTransportOptions<T>?  PlainTransportOptions  { get; init; }
    
    public static MediasoupOptionsContext<T> Default { get; } = new()
    {
        NumWorkers = Environment.ProcessorCount,
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
            ],
        },
        RouterOptions = new()
        {
            MediaCodecs =
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
                    Parameters = new()
                    {
                        { "x-google-start-bitrate", 1000 }
                    }
                },
                new()
                {
                    Kind      = MediaKind.VIDEO,
                    MimeType  = "video/VP9",
                    ClockRate = 90000,
                    Parameters = new()
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
                    Parameters = new()
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
                    Parameters = new()
                    {
                        { "packetization-mode", 1 },
                        { "profile-level-id", "42e01f" },
                        { "level-asymmetry-allowed", 1 },
                        { "x-google-start-bitrate", 1000 }
                    }
                }
            ],
        },
        WebRtcServerOptions = new()
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
        WebRtcTransportOptions = new()
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
            MaxSctpMessageSize              = 256 * 1024,
        },
        PlainTransportOptions = new()
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
    };
}