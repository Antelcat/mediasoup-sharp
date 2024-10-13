namespace MediasoupSharp.FlatBuffers.PlainTransport.T;

public class GetStatsResponseT
{
    public global::FlatBuffers.Transport.StatsT Base { get; set; }

    public bool RtcpMux { get; set; }

    public bool Comedia { get; set; }

    public global::FlatBuffers.Transport.TupleT Tuple { get; set; }

    public global::FlatBuffers.Transport.TupleT RtcpTuple { get; set; }
}
