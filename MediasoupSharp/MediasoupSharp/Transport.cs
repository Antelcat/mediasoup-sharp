using TransportProtocol = System.String;
using TransportTraceEventType = System.String;
using SctpState = System.String;

namespace MediasoupSharp;

public record TransportListenIp(
    //Listening IPv4 or IPv6.
    string Ip,
    //Announced IPv4 or IPv6 (useful when running mediasoup behind NAT with private IP
    string? AnnouncedIp);

public record TransportTuple(
    string LocalIp,
    Number LocalPort,
    string RemoteIp,
    Number RemotePort,
    TransportProtocol Protocol);

public record TransportTraceEventData(
    TransportTraceEventType Type,
    Number Timestamp,
    string Direction,
    object Info);

public record TransportEvents(
    List<object> RouterClose,
    List<object> ListenServerClose,
    List<TransportTraceEventData> Trace,
    List<object> @Close,
    List<Producer> @NewProducer,
    List<Producer> @ProducerClose,
    List<DataProducer> @NewDataProducer,
    List<DataProducer> @DataProducerClose,
    List<object> @listenServerClose);

public record TransportObserverEvents(
    List<object> Close,
    List<Producer> NewProducer,
    List<Consumer> NewConsumer,
    List<DataProducer> NewDataProducer,
    List<DataConsumer> NewDataConsumer,
    List<TransportTraceEventData> Trace);

public record TransportConstructorOptions<TTransportAppData>(
    TransportInternal Internal,
    TransportData Data,
    Channel Channel,
    PayloadChannel PayloadChannel,
    TTransportAppData? AppData,
    Func<RtpCapabilities> GetRouterRtpCapabilities,
    Func<string, Producer?> GetProducerById,
    Func<string, DataProducer> GetDataProducerById);

public record TransportInternal(string TransportId) : RouterInternal;

public interface ITransportData
{
}

public class Transport<TTransportAppData,
    TEvent, TObserverEvents> : EnhancedEventEmitter<TEvent>
    where TTransportAppData : AppData, new()
    where TEvent : TransportEvents
    where TObserverEvents : TransportObserverEvents
{
    protected internal readonly TransportInternal Internal;

    private readonly TransportData data;

    protected readonly Channel Channel;

    protected readonly PayloadChannel PayloadChannel;

    private bool closed;

    private TTransportAppData appData;

    private readonly Func<RtpCapabilities> getRouterRtpCapabilities;

    protected readonly Func<string, Producer?> GetProducerById;

    protected readonly Func<string, DataProducer?> GetDataProducerById;

    private readonly Dictionary<string, Producer> producers = new();

    private readonly Dictionary<string, DataProducer> dataProducers = new();

    private readonly Dictionary<string, DataConsumer> dataConsumers = new();

    private string cnameForProducers;

    private Number nextMidForConsumers = 0;

    private byte[]? sctpStreamIds;

    private Number nextSctpStreamId = 0;

    // Observer instance.
    private readonly EnhancedEventEmitter<TObserverEvents> observer = new();

    private Transport(TransportConstructorOptions<TTransportAppData> data) : this(
        data.Internal,
        data.Data,
        data.Channel,
        data.PayloadChannel,
        data.AppData,
        data.GetRouterRtpCapabilities,
        data.GetProducerById,
        data.GetDataProducerById)
    {
    }

    private Transport(
        TransportInternal @internal,
        TransportData data,
        Channel channel,
        PayloadChannel payloadChannel,
        TTransportAppData? appData,
        Func<RtpCapabilities> getRouterRtpCapabilities,
        Func<string, Producer?> getProducerById,
        Func<string, DataProducer?> getDataProducerById
    )
    {
        Internal = @internal;
        this.data = data;
        Channel = channel;
        PayloadChannel = payloadChannel;
        this.appData = appData ?? new TTransportAppData();
        this.getRouterRtpCapabilities = getRouterRtpCapabilities;
        GetProducerById = getProducerById;
        GetDataProducerById = getDataProducerById;
    }

    public string Id => Internal.TransportId;

    public bool Closed => closed;

    public TTransportAppData AppData
    {
        get => appData;
        set => appData = value;
    }

    public EnhancedEventEmitter<TObserverEvents> Observer => observer;

    public Channel ChannelForTesting => Channel;

    public void Close()
    {
        if (closed) return;
        closed = true;
        Channel.
    }
}