namespace MediasoupSharp.FlatBuffers.PipeTransport.T;

public class DumpResponseT
{
    public global::FlatBuffers.Transport.DumpT Base { get; set; }

    public global::FlatBuffers.Transport.TupleT Tuple { get; set; }

    public bool Rtx { get; set; }

    public global::FlatBuffers.SrtpParameters.SrtpParametersT SrtpParameters { get; set; }
}
