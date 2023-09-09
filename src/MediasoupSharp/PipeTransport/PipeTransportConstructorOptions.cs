using MediasoupSharp.Transport;

namespace MediasoupSharp.PipeTransport;

public record PipeTransportConstructorOptions<TPipeTransportAppData>
    : TransportConstructorOptions<TPipeTransportAppData>
{
    public PipeTransportData Data { get; set; }
}