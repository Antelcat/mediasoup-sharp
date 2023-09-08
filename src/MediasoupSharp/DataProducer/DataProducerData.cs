using MediasoupSharp.SctpParameters;

namespace MediasoupSharp.DataProducer;

public record DataProducerData
{
    public DataProducerType Type { get; set; }
    
    /// <summary>
    /// SCTP stream parameters.
    /// </summary>
    public SctpStreamParameters? SctpStreamParameters { get; init; }

    /// <summary>
    /// DataChannel label.
    /// </summary>
    public string Label { get; init; }

    /// <summary>
    /// DataChannel protocol.
    /// </summary>
    public string Protocol { get; init; }
}