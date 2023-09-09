using MediasoupSharp.RtpParameters;

namespace MediasoupSharp.Transport;

public record TransportConstructorOptions<TTransportAppData>
{
    public TransportInternal Internal { get; set; }
    public TransportData Data { get; set; }
    internal Channel.Channel Channel { get; set; }
    internal PayloadChannel.PayloadChannel PayloadChannel { get; set; }
    public TTransportAppData? AppData { get; set; }
    public Func<RtpCapabilities> GetRouterRtpCapabilities { get; set; }
    internal Func<string, Producer.Producer> GetProducerById { get; set; }
    internal Func<string, DataProducer.DataProducer> GetDataProducerById { get; set; }
}