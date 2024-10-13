using MediasoupSharp.FlatBuffers.SctpParameters.T;

namespace MediasoupSharp.DataProducer;

public class DataProducerData
{
    public global::FlatBuffers.DataProducer.Type Type { get; set; }

    /// <summary>
    /// SCTP stream parameters.
    /// </summary>
    public SctpStreamParametersT? SctpStreamParameters { get; init; }

    /// <summary>
    /// DataChannel label.
    /// </summary>
    public string Label { get; init; }

    /// <summary>
    /// DataChannel protocol.
    /// </summary>
    public string Protocol { get; init; }
}
