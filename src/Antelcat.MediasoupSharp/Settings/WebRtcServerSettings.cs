using FBS.Transport;

namespace Antelcat.MediasoupSharp.Settings;

public record WebRtcServerSettings
{
    public ListenInfoT[] ListenInfos { get; set; }
}