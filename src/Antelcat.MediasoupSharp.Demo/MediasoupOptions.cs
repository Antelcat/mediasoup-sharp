using Antelcat.MediasoupSharp.AspNetCore.Extensions;
using FBS.RtpParameters;
using FBS.Transport;

namespace Antelcat.MediasoupSharp.Demo;

public class MediasoupOptions<T>
{
    public int?                       NumWorkers             { get; set; } = Environment.ProcessorCount;
    public WorkerSettings<T>?         WorkerSettings         { get; init; }
    public RouterOptions<T>?          RouterOptions          { get; init; }
    public WebRtcServerOptions<T>?    WebRtcServerOptions    { get; init; }
    public WebRtcTransportOptions<T>? WebRtcTransportOptions { get; init; }
    public PlainTransportOptions<T>?  PlainTransportOptions  { get; init; }

    // mediasoup settings.
    public static MediasoupOptions<T> Default { get; } = new()
    {
        // Number of mediasoup workers to launch.
        NumWorkers = Environment.ProcessorCount,
        // mediasoup WorkerSettings.
        // See https://mediasoup.org/documentation/v3/mediasoup/api/#WorkerSettings
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
            DisableLiburing = false
        },
        // mediasoup Router options.
        // See https://mediasoup.org/documentation/v3/mediasoup/api/#RouterOptions
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
        // mediasoup WebRtcServer options for WebRTC endpoints (mediasoup-client,
        // libmediasoupclient).
        // See https://mediasoup.org/documentation/v3/mediasoup/api/#WebRtcServerOptions
        // NOTE: mediasoup-demo/server/lib/Room.js will increase this port for
        // each mediasoup Worker since each Worker is a separate process.
        WebRtcServerOptions = new()
        {
            ListenInfos =
            [
                new()
                {
                    Protocol         = Protocol.UDP,
                    Ip               = Env("MEDIASOUP_LISTEN_IP")    ?? "0.0.0.0",
                    AnnouncedAddress = Env("MEDIASOUP_ANNOUNCED_IP") ?? IPAddressExtensions.GetLocalIPv4String(),
                    Port             = 44444,
                    Flags            = new(),
                    PortRange        = new()
                },
                new()
                {
                    Protocol         = Protocol.TCP,
                    Ip               = Env("MEDIASOUP_LISTEN_IP")    ?? "0.0.0.0",
                    AnnouncedAddress = Env("MEDIASOUP_ANNOUNCED_IP") ?? IPAddressExtensions.GetLocalIPv4String(),
                    Port             = 44444,
                    Flags            = new(),
                    PortRange        = new()
                }
            ]
        },
        // mediasoup WebRtcTransport options for WebRTC endpoints (mediasoup-client,
        // libmediasoupclient).
        // See https://mediasoup.org/documentation/v3/mediasoup/api/#WebRtcTransportOptions
        WebRtcTransportOptions = new AdditionalWebRtcTransportOptions<T>
        {
            // listenInfos is not needed since webRtcServer is used.
            // However passing MEDIASOUP_USE_WEBRTC_SERVER=false will change it.
            ListenInfos =
            [
                new()
                {
                    Protocol = Protocol.UDP,

                    Ip               = Env("MEDIASOUP_LISTEN_IP")    ?? "0.0.0.0",
                    AnnouncedAddress = Env("MEDIASOUP_ANNOUNCED_IP") ?? IPAddressExtensions.GetLocalIPv4String(),
                    Flags            = new(),
                    PortRange = new()
                    {
                        Min = Env<ushort>("MEDIASOUP_MIN_PORT") ?? 40000,
                        Max = Env<ushort>("MEDIASOUP_MAX_PORT") ?? 49999
                    }
                },
                new()
                {
                    Protocol         = Protocol.TCP,
                    Ip               = Env("MEDIASOUP_LISTEN_IP")    ?? "0.0.0.0",
                    AnnouncedAddress = Env("MEDIASOUP_ANNOUNCED_IP") ?? IPAddressExtensions.GetLocalIPv4String(),
                    Flags            = new(),
                    PortRange = new()
                    {
                        Min = Env<ushort>("MEDIASOUP_MIN_PORT") ?? 40000,
                        Max = Env<ushort>("MEDIASOUP_MAX_PORT") ?? 49999
                    }
                }
            ],
            InitialAvailableOutgoingBitrate = 1_000_000,
            MinimumAvailableOutgoingBitrate = 600_000,
            MaxSctpMessageSize              = 262_144,
            // Additional options that are not part of WebRtcTransportOptions.
            MaxIncomingBitrate              = 1_500_000,
        },
        // mediasoup PlainTransport options for legacy RTP endpoints (FFmpeg,
        // GStreamer).
        // See https://mediasoup.org/documentation/v3/mediasoup/api/#PlainTransportOptions
        PlainTransportOptions = new()
        {
            ListenInfo = new()
            {
                Protocol         = Protocol.UDP,
                Ip               = Env("MEDIASOUP_LISTEN_IP")    ?? "0.0.0.0",
                AnnouncedAddress = Env("MEDIASOUP_ANNOUNCED_IP") ?? IPAddressExtensions.GetLocalIPv4String(),
                Flags            = new(),
                PortRange = new()
                {
                    Min = Env<ushort>("MEDIASOUP_MIN_PORT") ?? 40000,
                    Max = Env<ushort>("MEDIASOUP_MAX_PORT") ?? 49999
                }
            },
            MaxSctpMessageSize = 262144
        }
    };

    private static TNumber? Env<TNumber>(string variable) where TNumber : struct, IParsable<TNumber> =>
        Environment.GetEnvironmentVariable(variable) is { } str ? TNumber.Parse(str, null) : null;

    private static string? Env(string variable) => Environment.GetEnvironmentVariable(variable);
}

public record AdditionalWebRtcTransportOptions<T> : WebRtcTransportOptions<T>
{
    public uint? MinimumAvailableOutgoingBitrate { get; set; }

    // Additional options that are not part of WebRtcTransportOptions.
    public uint? MaxIncomingBitrate { get; set; }
}