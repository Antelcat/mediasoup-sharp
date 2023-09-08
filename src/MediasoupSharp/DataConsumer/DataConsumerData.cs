using MediasoupSharp.SctpParameters;

namespace MediasoupSharp.DataConsumer;

public class DataConsumerData
{
    /// <summary>
    /// Associated DataProducer id.
    /// </summary>
    public string DataProducerId { get; set; } = string.Empty;

    public DataConsumerType Type { get; set; }
    
    /// <summary>
    /// SCTP stream parameters.
    /// </summary>
    public SctpStreamParameters? SctpStreamParameters { get; set; }

    /// <summary>
    /// DataChannel label.
    /// </summary>
    public string Label { get; set; }

    /// <summary>
    /// DataChannel protocol.
    /// </summary>
    public string Protocol { get; set; }
}