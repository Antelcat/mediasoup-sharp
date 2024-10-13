namespace MediasoupSharp.FlatBuffers.WebRtcTransport.T;

public class WebRtcTransportOptionsT
{
    public global::FlatBuffers.Transport.OptionsT Base { get; set; }

    public global::FlatBuffers.WebRtcTransport.ListenUnion Listen { get; set; }

    public bool EnableUdp { get; set; }

    public bool EnableTcp { get; set; }

    public bool PreferUdp { get; set; }

    public bool PreferTcp { get; set; }
}