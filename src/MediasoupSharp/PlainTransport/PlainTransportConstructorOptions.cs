using MediasoupSharp.Transport;

namespace MediasoupSharp.PlainTransport;

public record PlainTransportConstructorOptions<TPlainTransportAppData>
    : TransportConstructorOptions<TPlainTransportAppData>
{
    public PlainTransportData Data { get; set; }
}