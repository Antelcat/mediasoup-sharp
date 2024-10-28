using FBS.Transport;

namespace Antelcat.MediasoupSharp.WebRtcServer;

public record WebRtcServerOptions
{
    /// <summary>
    /// Listen infos.
    /// </summary>
    public required ListenInfoT[] ListenInfos { get; set; }

    /// <summary>
    /// Custom application data.
    /// </summary>
    public Dictionary<string, object>? AppData { get; set; }
}