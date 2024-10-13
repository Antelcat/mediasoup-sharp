namespace MediasoupSharp.FlatBuffers.PlainTransport.T;

public class ConnectResponseT
{
    public global::FlatBuffers.Transport.TupleT Tuple { get; set; }

    public global::FlatBuffers.Transport.TupleT RtcpTuple { get; set; }

    public global::FlatBuffers.SrtpParameters.SrtpParametersT SrtpParameters { get; set; }
}
