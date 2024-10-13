using MediasoupSharp.FlatBuffers.SctpParameters.T;

namespace MediasoupSharp.FlatBuffers.Transport.T;

public class ConsumeDataRequestT
{
    public string DataConsumerId { get; set; }

    public string DataProducerId { get; set; }

    public global::FlatBuffers.DataProducer.Type Type { get; set; }

    public SctpStreamParametersT? SctpStreamParameters { get; set; }

    public string Label { get; set; }

    public string Protocol { get; set; }

    public bool Paused { get; set; }

    public List<ushort>? Subchannels { get; set; }
}