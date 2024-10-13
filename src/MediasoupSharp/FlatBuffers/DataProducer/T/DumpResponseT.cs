using MediasoupSharp.FlatBuffers.SctpParameters.T;

namespace MediasoupSharp.FlatBuffers.DataProducer.T;

public class DumpResponseT
{
    public string Id { get; set; }

    public global::FlatBuffers.DataProducer.Type Type { get; set; }

    public SctpStreamParametersT SctpStreamParameters { get; set; }

    public string Label { get; set; }

    public string Protocol { get; set; }

    public bool Paused { get; set; }
}
