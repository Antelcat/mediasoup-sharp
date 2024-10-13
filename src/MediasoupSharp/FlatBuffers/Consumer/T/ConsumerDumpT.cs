using MediasoupSharp.FlatBuffers.RtpStream.T;

namespace MediasoupSharp.FlatBuffers.Consumer.T;

public class ConsumerDumpT
{
    public BaseConsumerDumpT Base { get; set; }

    public List<DumpT> RtpStreams { get; set; }

    public short? PreferredSpatialLayer { get; set; }

    public short? TargetSpatialLayer { get; set; }

    public short? CurrentSpatialLayer { get; set; }

    public short? PreferredTemporalLayer { get; set; }

    public short? TargetTemporalLayer { get; set; }

    public short? CurrentTemporalLayer { get; set; }
}
