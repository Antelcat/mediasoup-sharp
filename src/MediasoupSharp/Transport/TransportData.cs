using MediasoupSharp.DirectTransport;
using MediasoupSharp.PipeTransport;
using MediasoupSharp.PlainTransport;
using MediasoupSharp.WebRtcTransport;

namespace MediasoupSharp.Transport;

public record TransportData : IWebRtcTransportData, IPlainTransportData, IPipeTransportData, IDirectTransportData
{
    private SrtpParameters.SrtpParameters? srtpParameters;
    public  string                         IceRole          { get; set; }
    public  IceParameters                  IceParameters    { get; set; }
    public  List<IceCandidate>             IceCandidates    { get; set; }
    public  IceState                       IceState         { get; set; }
    public  TransportTuple?                IceSelectedTuple { get; set; }
    public  DtlsParameters                 DtlsParameters   { get; set; }
    public  DtlsState                      DtlsState        { get; set; }
    public  string?                        DtlsRemoteCert   { get; set; }
    public  bool?                          RtcpMux          { get; set; }
    public  bool?                          Comedia          { get; set; }
    public  TransportTuple                 Tuple            { get; set; }
    public  TransportTuple?                RtcpTuple        { get; set; }
    public  SctpParameters.SctpParameters? SctpParameters   { get; set; }
    public  SctpState?                     SctpState        { get; set; }


    public bool                           Rtx { get; set; }
    public SrtpParameters.SrtpParameters? SrtpParameters   { get; set; }
}