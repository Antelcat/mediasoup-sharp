using MediasoupSharp.Transport;

namespace MediasoupSharp.DirectTransport;

public record DirectTransportObserverEvents : TransportObserverEvents
{
    public byte[] Rtcp { get; set; }
}