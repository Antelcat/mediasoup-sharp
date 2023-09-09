using MediasoupSharp.RtpParameters;

namespace MediasoupSharp.Consumer;

public record ConsumerData
{
    /// <summary>
    /// Associated Producer id.
    /// </summary>
    public string ProducerId { get; set; }

    /// <summary>
    /// Media kind.
    /// </summary>
    public MediaKind Kind { get; set; }

    /// <summary>
    /// RTP parameters.
    /// </summary>
    public RtpParameters.RtpParameters RtpParameters { get; set; }

    /// <summary>
    /// Consumer type.
    /// </summary>
    public ConsumerType Type { get; set; }
   
}