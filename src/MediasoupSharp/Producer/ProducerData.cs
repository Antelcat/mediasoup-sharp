using MediasoupSharp.RtpParameters;

namespace MediasoupSharp.Producer;

public class ProducerData
{
    /// <summary>
    /// Media kind.
    /// </summary>
    public MediaKind Kind { get; set; }

    /// <summary>
    /// RTP parameters.
    /// </summary>
    public RtpParameters.RtpParameters RtpParameters { get; set; }

    /// <summary>
    /// Producer type.
    /// </summary>
    public ProducerType Type { get; set; }

    /// <summary>
    /// Consumable RTP parameters.
    /// </summary>
    public RtpParameters.RtpParameters ConsumableRtpParameters { get; set; }
}