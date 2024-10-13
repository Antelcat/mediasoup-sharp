namespace MediasoupSharp.FlatBuffers.PlainTransport.T;

public class DumpResponseT
{
    public global::FlatBuffers.Transport.DumpT Base { get; set; }

    public bool RtcpMux { get; set; }

    public bool Comedia { get; set; }

    public global::FlatBuffers.Transport.TupleT Tuple { get; set; }

    public global::FlatBuffers.Transport.TupleT RtcpTuple { get; set; }

    public global::FlatBuffers.SrtpParameters.SrtpParametersT SrtpParameters { get; set; }
}
