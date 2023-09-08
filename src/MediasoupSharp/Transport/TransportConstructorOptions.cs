using MediasoupSharp.RtpParameters;

namespace MediasoupSharp.Transport;

public record TransportConstructorOptions<TTransportAppData>
{
    public TransportInternal Internal { get; set; }
    public TransportData Data { get; set; }
    public Channel.Channel Channel { get; set; }
    public PayloadChannel.PayloadChannel PayloadChannel { get; set; }
    public TTransportAppData? AppData { get; set; }
    public Func<RtpCapabilities> GetRouterRtpCapabilities { get; set; }
    public Func<string, Producer.Producer<TTransportAppData>> GetProducerById { get; set; }
    internal Func<string, DataProducer.DataProducer> GetDataProducerById { get; set; }
}