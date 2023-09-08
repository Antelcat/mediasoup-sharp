using MediasoupSharp.Transport;

namespace MediasoupSharp.DirectTransport;

public record DirectTransportEvents : TransportEvents
{
    public byte[] Rtcp { get; set; }
}