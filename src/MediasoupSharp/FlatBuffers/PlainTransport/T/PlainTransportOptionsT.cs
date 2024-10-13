using MediasoupSharp.FlatBuffers.Transport.T;

namespace MediasoupSharp.FlatBuffers.PlainTransport.T;

public class PlainTransportOptionsT
{
    public global::FlatBuffers.Transport.OptionsT Base { get; set; }

    public ListenInfoT ListenInfo { get; set; }

    /// <summary>
    /// RtcpListenInfo. Nullable.
    /// </summary>
    /// <value></value>
    public ListenInfoT? RtcpListenInfo { get; set; }

    public bool RtcpMux { get; set; }

    public bool Comedia { get; set; }

    public bool EnableSrtp { get; set; }

    public global::FlatBuffers.SrtpParameters.SrtpCryptoSuite? SrtpCryptoSuite { get; set; }
}
