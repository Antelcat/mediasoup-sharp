﻿using MediasoupSharp.Transport;

namespace MediasoupSharp.WebRtcTransport;

public record WebRtcTransportObserverEvents : TransportObserverEvents
{
    public Tuple<IceState> Icestatechange { get; set; }
    public Tuple<TransportTuple> Iceselectedtuplechange { get; set; }
    public Tuple<DtlsState> Dtlsstatechange { get; set; }
    public Tuple<SctpState> Sctpstatechange { get; set; }
}