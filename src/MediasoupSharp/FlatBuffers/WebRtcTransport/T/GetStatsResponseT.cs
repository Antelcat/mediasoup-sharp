namespace MediasoupSharp.FlatBuffers.WebRtcTransport.T;

public class GetStatsResponseT
{
    public global::FlatBuffers.Transport.StatsT Base { get; set; }

    public global::FlatBuffers.WebRtcTransport.IceRole IceRole { get; set; }

    public global::FlatBuffers.WebRtcTransport.IceState IceState { get; set; }

    public global::FlatBuffers.Transport.TupleT IceSelectedTuple { get; set; }

    public global::FlatBuffers.WebRtcTransport.DtlsState DtlsState { get; set; }
}