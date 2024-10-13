namespace MediasoupSharp.FlatBuffers.PipeTransport.T;

public class ConnectRequestT
{
    public string Ip { get; set; }

    public ushort? Port { get; set; }

    public global::FlatBuffers.SrtpParameters.SrtpParametersT SrtpParameters { get; set; }
}
