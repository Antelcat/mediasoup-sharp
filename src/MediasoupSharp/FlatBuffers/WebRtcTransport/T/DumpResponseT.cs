namespace MediasoupSharp.FlatBuffers.WebRtcTransport.T;

public class DumpResponseT
{
    public global::FlatBuffers.Transport.DumpT Base { get; set; }

    public global::FlatBuffers.WebRtcTransport.IceRole IceRole { get; set; }

    public IceParametersT IceParameters { get; set; }

    public List<IceCandidateT> IceCandidates { get; set; }

    public global::FlatBuffers.WebRtcTransport.IceState IceState { get; set; }

    public global::FlatBuffers.Transport.TupleT? IceSelectedTuple { get; set; }

    public DtlsParametersT DtlsParameters { get; set; }

    public global::FlatBuffers.WebRtcTransport.DtlsState DtlsState { get; set; }
}