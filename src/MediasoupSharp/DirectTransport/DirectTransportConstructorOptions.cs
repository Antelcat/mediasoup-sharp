using MediasoupSharp.Transport;

namespace MediasoupSharp.DirectTransport;

public record DirectTransportConstructorOptions<TDirectTransportAppData> 
    : TransportConstructorOptions<TDirectTransportAppData>
{
    public DirectTransportData Data { get; set; }
}