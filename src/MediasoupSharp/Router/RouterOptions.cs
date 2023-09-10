using MediasoupSharp.RtpParameters;

namespace MediasoupSharp.Router;

public class RouterOptions<TRouterAppData>
{
    /// <summary>
    /// Router media codecs.
    /// </summary>
    public List<RtpCodecCapability>? MediaCodecs { get; set; }

    /// <summary>
    /// Custom application data.
    /// </summary>
    public TRouterAppData? AppData { get; set; }
}