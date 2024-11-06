using Antelcat.MediasoupSharp.AspNetCore;
using FBS.RtpParameters;
using FBS.Transport;

namespace Antelcat.MediasoupSharp.Demo;

public record WebRtcTransportOptions<T> : Antelcat.MediasoupSharp.WebRtcTransportOptions<T>
{
    public uint? MinimumAvailableOutgoingBitrate { get; set; }

    // Additional options that are not part of WebRtcTransportOptions.
    public uint? MaxIncomingBitrate { get; set; }
}

public class MediasoupOptions<T> : MediasoupOptionsContext<T>
{
    public new WebRtcTransportOptions<T>? WebRtcTransportOptions
    {
        get => webRtcTransportOptions;
        init
        {
            webRtcTransportOptions      = value;
            base.WebRtcTransportOptions = value;
        }
    }

    private readonly WebRtcTransportOptions<T>? webRtcTransportOptions;
}