using System.Reflection;
using Antelcat.AutoGen.ComponentModel.Diagnostic;
using Antelcat.MediasoupSharp.Internals.Extensions;
using FBS.Notification;
using FBS.PipeTransport;
using FBS.Request;
using FBS.SctpAssociation;
using FBS.SctpParameters;
using FBS.SrtpParameters;
using FBS.Transport;
using Microsoft.Extensions.Logging;

namespace Antelcat.MediasoupSharp;

using PipeTransportObserver = EnhancedEventEmitter<PipeTransportObserverEvents>;

public class PipeTransportOptions<TPipeTransportAppData>
{
    /// <summary>
    /// Listening Information.
    /// </summary>
    public ListenInfoT ListenInfo { get; set; }

    /// <summary>
    /// Create a SCTP association. Default false.
    /// </summary>
    public bool EnableSctp { get; set; }

    /// <summary>
    /// SCTP streams number.
    /// </summary>
    public NumSctpStreamsT NumSctpStreams { get; set; } = new() { Os = 1024, Mis = 1024 };

    /// <summary>
    /// Maximum allowed size for SCTP messages sent by DataProducers.
    /// Default 268435456.
    /// </summary>
    public uint MaxSctpMessageSize { get; set; } = 268435456;

    /// <summary>
    /// Maximum SCTP send buffer used by DataConsumers.
    /// Default 268435456.
    /// </summary>
    public uint SctpSendBufferSize { get; set; } = 268435456;

    /// <summary>
    /// Enable RTX and NACK for RTP retransmission. Useful if both Routers are
    /// located in different hosts and there is packet lost in the link. For this
    /// to work, both PipeTransports must enable this setting. Default false.
    /// </summary>
    public bool EnableRtx { get; set; }

    /// <summary>
    /// Enable SRTP. Useful to protect the RTP and RTCP traffic if both Routers
    /// are located in different hosts. For this to work, connect() must be called
    /// with remote SRTP parameters. Default false.
    /// </summary>
    public bool EnableSrtp { get; set; }

    /// <summary>
    /// Custom application data.
    /// </summary>
    public TPipeTransportAppData? AppData { get; set; }
}

public class PipeConsumerOptions<TConsumerAppData>
{
    /// <summary>
    /// The id of the Producer to consume.
    /// </summary>
    public string ProducerId { get; set; }

    /// <summary>
    /// Custom application data.
    /// </summary>
    public TConsumerAppData? AppData { get; set; }
}

public class PipeTransportEvents : TransportEvents
{
    public SctpState sctpstatechange;
}

public class PipeTransportObserverEvents : TransportObserverEvents
{
    public SctpState sctpstatechange;
}

public class PipeTransportConstructorOptions<TPipeTransportAppData>(PipeTransportData data)
    : TransportConstructorOptions<TPipeTransportAppData>(data)
{
    public override PipeTransportData Data { get; } = data;
}

[AutoMetadataFrom(typeof(PipeTransportData), MemberTypes.Property,
    Leading =
        $"public static implicit operator {nameof(PipeTransportData)}(global::{nameof(FBS)}.{nameof(FBS.PipeTransport)}.{nameof(DumpResponseT)} source) => new (source.Base){{",
    Template = "{Name} = source.{Name},",
    Trailing = "};")]
[AutoMetadataFrom(typeof(PipeTransportData), MemberTypes.Property,
    Leading =
        $"public static implicit operator global::{nameof(FBS)}.{nameof(FBS.PipeTransport)}.{nameof(DumpResponseT)}({nameof(PipeTransportData)} source) => new (){{",
    Template = "{Name} = source.{Name},",
    Trailing = "Base = source  };")]
public partial class PipeTransportData(DumpT dump) : TransportBaseData(dump)
{
    public TupleT Tuple { get; set; }

    public bool Rtx { get; set; }

    public SrtpParametersT? SrtpParameters { get; set; }
}

[AutoExtractInterface(Interfaces = [typeof(ITransport)], Exclude = [nameof(ConsumeAsync)])]
public class PipeTransport<TPipeTransportAppData>
    : Transport<TPipeTransportAppData, PipeTransportEvents, PipeTransportObserver>, IPipeTransport
    where TPipeTransportAppData : new()
{
    /// <summary>
    /// Logger.
    /// </summary>
    private readonly ILogger logger = new Logger<PipeTransport<TPipeTransportAppData>>();

    /// <summary>
    /// PipeTransport data.
    /// </summary>
    public override PipeTransportData Data { get; }

    /// <summary>
    /// <para>Events:</para> 
    /// <para>@emits <see cref="PipeTransportEvents.sctpstatechange"/> - (sctpState: SctpState)</para>
    /// <para>@emits <see cref="PipeTransportEvents.trace"/> - (trace: TransportTraceEventData)</para>
    /// <para>Observer events:</para>
    /// <para>@emits <see cref="PipeTransportObserverEvents.close"/></para>
    /// <para>@emits <see cref="PipeTransportObserverEvents.newproducer"/> - (producer: Producer)</para>
    /// <para>@emits <see cref="PipeTransportObserverEvents.newconsumer"/> - (consumer: Consumer)</para>
    /// <para>@emits <see cref="PipeTransportObserverEvents.newdataproducer"/> - (dataProducer: DataProducer)</para>
    /// <para>@emits <see cref="PipeTransportObserverEvents.newdataconsumer"/> - (dataConsumer: DataConsumer)</para>
    /// <para>@emits <see cref="PipeTransportObserverEvents.sctpstatechange"/> - (sctpState: SctpState)</para>
    /// <para>@emits trace - (trace: TransportTraceEventData)</para>
    /// </summary>
    public PipeTransport(PipeTransportConstructorOptions<TPipeTransportAppData> options)
        : base(options, new PipeTransportObserver())
    {
        Data = options.Data;

        HandleWorkerNotifications();
    }

    /// <summary>
    /// Close the PipeTransport.
    /// </summary>
    protected override Task OnCloseAsync()
    {
        if (Data.SctpState.HasValue)
        {
            Data.SctpState = FBS.SctpAssociation.SctpState.CLOSED;
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Router was closed.
    /// </summary>
    protected override Task OnRouterClosedAsync()
    {
        return OnCloseAsync();
    }

    /// <summary>
    /// Dump Transport.
    /// </summary>
    protected override async Task<object> OnDumpAsync()
    {
        // Build Request
        var bufferBuilder = Channel.BufferPool.Get();

        var response =
            await Channel.RequestAsync(bufferBuilder, Method.TRANSPORT_DUMP, null, null, Internal.TransportId);
        var data = response.Value.BodyAsPipeTransport_DumpResponse().UnPack();

        return data;
    }

    /// <summary>
    /// Get Transport stats.
    /// </summary>
    protected override async Task<object[]> OnGetStatsAsync()
    {
        // Build Request
        var bufferBuilder = Channel.BufferPool.Get();

        var response =
            await Channel.RequestAsync(bufferBuilder, Method.TRANSPORT_GET_STATS, null, null, Internal.TransportId);
        var data = response.Value.BodyAsPipeTransport_GetStatsResponse().UnPack();

        return [data];
    }

    /// <summary>
    /// Provide the PipeTransport remote parameters.
    /// </summary>
    protected override async Task OnConnectAsync(object parameters)
    {
        logger.LogDebug("OnConnectAsync() | PipeTransport:{TransportId}", Id);

        if (parameters is not ConnectRequestT connectRequestT)
        {
            throw new Exception($"{nameof(parameters)} type is not FBS.PipeTransport.ConnectRequestT");
        }

        // Build Request
        var bufferBuilder = Channel.BufferPool.Get();

        var connectRequestOffset = ConnectRequest.Pack(bufferBuilder, connectRequestT);

        var response = await Channel.RequestAsync(bufferBuilder, Method.PIPETRANSPORT_CONNECT,
            FBS.Request.Body.PipeTransport_ConnectRequest,
            connectRequestOffset.Value,
            Internal.TransportId);

        /* Decode Response. */
        var data = response.Value.BodyAsPipeTransport_ConnectResponse().UnPack();

        // Update data.
        Data.Tuple = data.Tuple;
    }

    
    /// <summary>
    /// Create a Consumer.
    /// </summary>
    /// <param name="consumerOptions">注意：由于强类型的原因，这里使用的是 ConsumerOptions 类而不是 PipConsumerOptions 类</param>
    public override async Task<Consumer<TConsumerAppData>> ConsumeAsync<TConsumerAppData>(
        ConsumerOptions<TConsumerAppData> consumerOptions)
    {
        logger.LogDebug("ConsumeAsync()");

        if (consumerOptions.ProducerId.IsNullOrWhiteSpace())
        {
            throw new Exception("missing producerId");
        }

        var producer = await GetProducerById(consumerOptions.ProducerId) ??
                       throw new Exception($"Producer with id {consumerOptions.ProducerId} not found");

        // This may throw.
        var rtpParameters = Ortc.GetPipeConsumerRtpParameters(producer.Data.ConsumableRtpParameters, Data.Rtx);

        var consumerId = Guid.NewGuid().ToString();

        // Build Request
        var bufferBuilder = Channel.BufferPool.Get();

        var consumeRequest = new ConsumeRequestT
        {
            ProducerId             = consumerOptions.ProducerId,
            ConsumerId             = consumerId,
            Kind                   = producer.Data.Kind,
            RtpParameters          = rtpParameters.SerializeRtpParameters(),
            Type                   = FBS.RtpParameters.Type.PIPE,
            ConsumableRtpEncodings = producer.Data.ConsumableRtpParameters.Encodings,
        };

        var consumeRequestOffset = ConsumeRequest.Pack(bufferBuilder, consumeRequest);

        var response = await Channel.RequestAsync(bufferBuilder, Method.TRANSPORT_CONSUME,
            FBS.Request.Body.Transport_ConsumeRequest,
            consumeRequestOffset.Value,
            Internal.TransportId);

        /* Decode Response. */
        var responseData = response.Value.BodyAsTransport_ConsumeResponse().UnPack();

        var consumerData = new ConsumerData
        {
            ProducerId    = consumerOptions.ProducerId,
            Kind          = producer.Data.Kind,
            RtpParameters = rtpParameters,
            Type          = producer.Data.Type,
        };

        var score = new FBS.Consumer.ConsumerScoreT
        {
            Score          = 10,
            ProducerScore  = 10,
            ProducerScores = []
        };

        var consumer = new Consumer<TConsumerAppData>(
            new ConsumerInternal
            {
                RouterId    = Internal.RouterId,
                TransportId = Internal.TransportId,
                ConsumerId  = consumerId
            },
            consumerData,
            Channel,
            consumerOptions.AppData,
            responseData.Paused,
            responseData.ProducerPaused,
            score, // Not `responseData.Score`
            responseData.PreferredLayers
        );

        consumer.On(
            "@close",
            async () =>
            {
                await ConsumersLock.WaitAsync();
                try
                {
                    Consumers.Remove(consumer.Id);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "@close");
                }
                finally
                {
                    ConsumersLock.Set();
                }
            }
        );
        consumer.On(
            "@producerclose",
            async () =>
            {
                await ConsumersLock.WaitAsync();
                try
                {
                    Consumers.Remove(consumer.Id);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "@producerclose");
                }
                finally
                {
                    ConsumersLock.Set();
                }
            }
        );

        await ConsumersLock.WaitAsync();
        try
        {
            Consumers[consumer.Id] = consumer;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "ConsumeAsync()");
        }
        finally
        {
            ConsumersLock.Set();
        }

        // Emit observer event.
        Observer.Emit("newconsumer", consumer);

        return consumer;
    }

    #region Event Handlers

    private void HandleWorkerNotifications()
    {
        Channel.OnNotification += OnNotificationHandle;
    }

    private void OnNotificationHandle(string handlerId, Event @event, Notification notification)
    {
        if (handlerId != Internal.TransportId)
        {
            return;
        }

        switch (@event)
        {
            case Event.TRANSPORT_SCTP_STATE_CHANGE:
            {
                var sctpStateChangeNotification = notification.BodyAsTransport_SctpStateChangeNotification().UnPack();

                Data.SctpState = sctpStateChangeNotification.SctpState;

                Emit("sctpstatechange", Data.SctpState);

                // Emit observer event.
                Observer.Emit("sctpstatechange", Data.SctpState);

                break;
            }
            case Event.TRANSPORT_TRACE:
            {
                var traceNotification = notification.BodyAsTransport_TraceNotification().UnPack();

                Emit("trace", traceNotification);

                // Emit observer event.
                Observer.Emit("trace", traceNotification);

                break;
            }
            default:
            {
                logger.LogError("OnNotificationHandle() | PipeTransport:{TransportId} Ignoring unknown event:{@event}",
                    Id, @event);
                break;
            }
        }
    }

    #endregion Event Handlers
}