﻿using MediasoupSharp.Transport;

namespace MediasoupSharp.PlainTransport;

public record PlainTransportObserverEvents : TransportObserverEvents
{
    public Tuple<TransportTuple> Tuple           { get; set; }
    public Tuple<TransportTuple> Rtcptuple       { get; set; }
    public Tuple<SctpState>      Sctpstatechange { get; set; }
}