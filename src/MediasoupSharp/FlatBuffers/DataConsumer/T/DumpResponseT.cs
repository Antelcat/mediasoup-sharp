using MediasoupSharp.FlatBuffers.SctpParameters.T;

namespace MediasoupSharp.FlatBuffers.DataConsumer.T;

public class DumpResponseT
{
    public string Id { get; set; }

    public string DataProducerId { get; set; }

    public global::FlatBuffers.DataProducer.Type Type { get; set; }

    public SctpStreamParametersT SctpStreamParameters { get; set; }

    public string Label { get; set; }

    public string Protocol { get; set; }

    public uint BufferedAmountLowThreshold { get; set; }

    public bool Paused { get; set; }

    public bool DataProducerPaused { get; set; }

    public List<ushort> Subchannels { get; set; }
}
