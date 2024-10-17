using FBS.RtpParameters;
using Type = FBS.RtpParameters.Type;

namespace MediasoupSharp.Producer;

public class ProducerData
{
    /// <summary>
    /// Media kind.
    /// </summary>
    public MediaKind Kind { get; init; }

    /// <summary>
    /// RTP parameters.
    /// </summary>
    public RtpParameters.RtpParameters RtpParameters { get; init; }

    /// <summary>
    /// Producer type.
    /// </summary>
    public Type Type { get; init; }

    /// <summary>
    /// Consumable RTP parameters.
    /// </summary>
    public RtpParameters.RtpParameters ConsumableRtpParameters { get; init; }
}