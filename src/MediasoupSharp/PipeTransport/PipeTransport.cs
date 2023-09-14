using MediasoupSharp.Consumer;
using MediasoupSharp.Transport;
using Microsoft.Extensions.Logging;

namespace MediasoupSharp.PipeTransport;

public interface IPipeTransport : ITransport
{
    TransportTuple Tuple { get; }
    
    SctpParameters.SctpParameters? SctpParameters { get; }
    
    SrtpParameters.SrtpParameters? SrtpParameters { get; }
}


internal class PipeTransport<TPipeTransportAppData> 
    : Transport<TPipeTransportAppData, PipeTransportEvents, PipeTransportObserverEvents> , IPipeTransport 
{
    private readonly ILogger? logger;
    
    /// <summary>
    /// PipeTransport data.
    /// </summary>
    private readonly PipeTransportData data;

    public PipeTransport(
        PipeTransportConstructorOptions<TPipeTransportAppData> options,
        ILoggerFactory? loggerFactory = null) 
        : base(options,loggerFactory)
    {
        logger = loggerFactory?.CreateLogger(GetType());
        
        data = options.Data with { };

        HandleWorkerNotifications();
    }

    public TransportTuple Tuple => data.Tuple;

    public SctpParameters.SctpParameters? SctpParameters => data.SctpParameters;

    public SctpState? SctpState => data.SctpState;

    public SrtpParameters.SrtpParameters? SrtpParameters => data.SrtpParameters;
    
    /// <summary>
    /// Close the PipeTransport.
    /// </summary>
    public override void Close()
    {
        if (Closed)
        {
            return;
        }

        if (data.SctpState != null)
        {
            data.SctpState = Transport.SctpState.closed;
        }
        
        base.Close();
    }

    /// <summary>
    /// Router was closed.
    /// </summary>
    public override void RouterClosed()
    {
        if (Closed)
        {
            return;
        }

        if (data.SctpState != null)
        {
            data.SctpState = Transport.SctpState.closed;
        }
        
        base.RouterClosed();
    }

    /// <summary>
    /// Get PipeTransport stats.
    /// </summary>
    /// <returns></returns>
    public async Task<List<PipeTransportStat>> GetStats()
    {
        logger?.LogDebug("getStats()");

        return (await Channel.Request("transport.getStats", Internal.TransportId) as List<PipeTransportStat>)!;
    }

    /// <summary>
    /// Provide the PipeTransport remote parameters.
    /// </summary>
    /// <param name="parameters"></param>
    /// <returns></returns>
    public override async Task ConnectAsync(object parameters)
    {
        logger?.LogDebug("ConnectAsync()");

        var data =
            await Channel.Request("transport.connect", Internal.TransportId, parameters) as dynamic;

        // Update data.
        this.data.Tuple = data!.Tuple;
    }


    /// <summary>
    /// Create a Consumer.
    /// </summary>
    /// <param name="consumerOptions">注意：由于强类型的原因，这里使用的是 ConsumerOptions 类而不是 PipConsumerOptions 类</param>
    /// <returns></returns>
    public async Task<Consumer<TConsumerAppData>> ConsumeAsync<TConsumerAppData>(PipeConsumerOptions<TConsumerAppData> consumerOptions)
    {
        var producerId = consumerOptions.ProducerId;
        var appData    = consumerOptions.AppData;
        logger?.LogDebug("ConsumeAsync()");

        if (producerId.IsNullOrEmpty())
        {
            throw new TypeError("missing producerId");
        }

        var producer = GetProducerById(consumerOptions.ProducerId);
        if (producer == null)
        {
            throw new TypeError($"Producer with id {consumerOptions.ProducerId} not found");
        }

        // This may throw.
        var rtpParameters = ORTC.Ortc.GetPipeConsumerRtpParameters(producer.ConsumableRtpParameters, this.data.Rtx);
        // TODO : Naming
        var reqData = new
        {
            ConsumerId = Guid.NewGuid().ToString(),
            producerId,
            Kind                   = producer.Kind,
            RtpParameters          = rtpParameters,
            Type                   = ConsumerType.pipe,
            ConsumableRtpEncodings = producer.ConsumableRtpParameters.Encodings,
        };

        var status = await Channel.Request("transport.consume", Internal.TransportId, reqData) as dynamic;

        var data = new ConsumerData
        {
            ProducerId    = producerId,
            Kind          = producer.Kind,
            RtpParameters = rtpParameters,
            Type          = ConsumerType.pipe
        };

        // TODO : Naming
        var consumer = new Consumer<TConsumerAppData>(
            new ConsumerInternal
            {
                RouterId    = Internal.RouterId,
                TransportId = Internal.TransportId,
                ConsumerId  = reqData.ConsumerId
            },
            data,
            Channel,
            PayloadChannel,
            appData,
            status!.Paused,
            status.ProducerPaused);

        Consumers[consumer.Id] = consumer;

        consumer.On("@close", async _ =>
        {
            Consumers.Remove(consumer.Id);
        });
        consumer.On("@producerclose", async _ =>
        {
            Consumers.Remove(consumer.Id);
        });
       
        // Emit observer event.
        await Observer.Emit("newconsumer", consumer);
        return consumer;
    }

    private void HandleWorkerNotifications()
    {
        Channel.On(Internal.TransportId, async args =>
        {
            var @event = args![0] as string;
            var data   = args[1] as dynamic;
            switch (@event)
            {
                case "sctpstatechange":
                {
                    var sctpState = (SctpState)data.sctpState;

                    data.SctpState = sctpState;

                    await SafeEmit("sctpstatechange", sctpState);

                    // Emit observer event.
                    await Observer.SafeEmit("sctpstatechange", sctpState);

                    break;
                }

                case "trace":
                {
                    var trace = (data as TransportTraceEventData)!;

                    await SafeEmit("trace", trace);

                    // Emit observer event.
                    await Observer.SafeEmit("trace", trace);

                    break;
                }

                default:
                {
                    logger?.LogError("ignoring unknown event {E}", @event);
                    break;
                }
            }
        });
    }
}