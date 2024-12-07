using System.Reflection;
using Antelcat.AutoGen.ComponentModel.Diagnostic;
using Antelcat.MediasoupSharp.Internals.Extensions;
using Antelcat.MediasoupSharp.FBS.Notification;
using Antelcat.MediasoupSharp.FBS.PipeTransport;
using Antelcat.MediasoupSharp.FBS.Request;
using Antelcat.MediasoupSharp.FBS.SctpAssociation;
using Antelcat.MediasoupSharp.FBS.SctpParameters;
using Antelcat.MediasoupSharp.FBS.SrtpParameters;
using Antelcat.MediasoupSharp.FBS.Transport;
using Microsoft.Extensions.Logging;

namespace Antelcat.MediasoupSharp;

public class PipeTransportConstructorOptions<TPipeTransportAppData>(PipeTransportData data)
    : TransportConstructorOptions<TPipeTransportAppData>(data)
{
    public override PipeTransportData Data => base.Data.Sure<PipeTransportData>();
}

[AutoMetadataFrom(typeof(PipeTransportData), MemberTypes.Property,
    Leading =
        $"public static implicit operator {nameof(PipeTransportData)}(global::Antelcat.MediasoupSharp.FBS.PipeTransport.{nameof(DumpResponseT)} source) => new (source.Base){{",
    Template = "{Name} = source.{Name},",
    Trailing = "};")]
[AutoMetadataFrom(typeof(PipeTransportData), MemberTypes.Property,
    Leading =
        $"public static implicit operator global::Antelcat.MediasoupSharp.FBS.PipeTransport.{nameof(DumpResponseT)}({nameof(PipeTransportData)} source) => new (){{",
    Template = "{Name} = source.{Name},",
    Trailing = "Base = source  };")]
public partial record PipeTransportData : TransportBaseData
{
    public PipeTransportData(DumpT dump) : base(dump) { }
    
    public required TupleT Tuple { get; set; }

    public bool Rtx { get; set; }

    public SrtpParametersT? SrtpParameters { get; set; }
}

[AutoExtractInterface(
    NamingTemplate = nameof(IPipeTransport),
    Interfaces = [typeof(ITransport), typeof(IEnhancedEventEmitter<PipeTransportObserver>)],
    Exclude = [nameof(ConsumeAsync)])]
public class PipeTransportImpl<TPipeTransportAppData>
    : TransportImpl<
            TPipeTransportAppData, 
            PipeTransportEvents, 
            PipeTransportObserver
        >, IPipeTransport<TPipeTransportAppData>
    where TPipeTransportAppData : new()
{
    /// <summary>
    /// Logger.
    /// </summary>
    private readonly ILogger logger = new Logger<IPipeTransport>();

    /// <summary>
    /// PipeTransport data.
    /// </summary>
    public override PipeTransportData Data { get; }

    public override PipeTransportObserver Observer => base.Observer.Sure<PipeTransportObserver>();

    /// <summary>
    /// <para>Events : <see cref="PipeTransportEvents"/></para> 
    /// <para>Observer events : <see cref="TransportObserverEvents"/></para>
    /// </summary>
    public PipeTransportImpl(PipeTransportConstructorOptions<TPipeTransportAppData> options)
        : base(options, new PipeTransportObserver())
    {
        Data = options.Data with { };

        HandleWorkerNotifications();
        HandleListenerError();
    }

    /// <summary>
    /// Close the PipeTransport.
    /// </summary>
    protected override Task OnCloseAsync()
    {
        if (Data.SctpState.HasValue)
        {
            Data.SctpState = SctpState.CLOSED;
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
        var data = response.NotNull().BodyAsPipeTransport_DumpResponse().UnPack();

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
        var data = response.NotNull().BodyAsPipeTransport_GetStatsResponse().UnPack();

        return [data];
    }

    /// <summary>
    /// Provide the PipeTransport remote parameters.
    /// </summary>
    protected override async Task OnConnectAsync(object parameters)
    {
        logger.LogDebug($"{nameof(OnConnectAsync)}() | PipeTransport:{{TransportId}}", Id);

        if (parameters is not ConnectRequestT connectRequestT)
        {
            throw new Exception($"{nameof(parameters)} type is not Antelcat.MediasoupSharp.FBS.PipeTransport.ConnectRequestT");
        }

        // Build Request
        var bufferBuilder = Channel.BufferPool.Get();

        var connectRequestOffset = ConnectRequest.Pack(bufferBuilder, connectRequestT);

        var response = await Channel.RequestAsync(bufferBuilder, Method.PIPETRANSPORT_CONNECT,
            Antelcat.MediasoupSharp.FBS.Request.Body.PipeTransport_ConnectRequest,
            connectRequestOffset.Value,
            Internal.TransportId);

        /* Decode Response. */
        var data = response.NotNull().BodyAsPipeTransport_ConnectResponse().UnPack();

        // Update data.
        Data.Tuple = data.Tuple;
    }

    /// <summary>
    /// Create a Consumer.
    /// </summary>
    /// <param name="consumerOptions">注意：由于强类型的原因，这里使用的是 ConsumerOptions 类而不是 PipConsumerOptions 类</param>
    public override async Task<ConsumerImpl<TConsumerAppData>> ConsumeAsync<TConsumerAppData>(
        ConsumerOptions<TConsumerAppData> consumerOptions)
    {
        logger.LogDebug($"{nameof(ConsumeAsync)}()");

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
            Type                   = Antelcat.MediasoupSharp.FBS.RtpParameters.Type.PIPE,
            ConsumableRtpEncodings = producer.Data.ConsumableRtpParameters.Encodings
        };

        var consumeRequestOffset = ConsumeRequest.Pack(bufferBuilder, consumeRequest);

        var response = await Channel.RequestAsync(bufferBuilder, Method.TRANSPORT_CONSUME,
            Antelcat.MediasoupSharp.FBS.Request.Body.Transport_ConsumeRequest,
            consumeRequestOffset.Value,
            Internal.TransportId);

        /* Decode Response. */
        var responseData = response.NotNull().BodyAsTransport_ConsumeResponse().UnPack();

        var consumerData = new ConsumerData
        {
            ProducerId    = consumerOptions.ProducerId,
            Kind          = producer.Data.Kind,
            RtpParameters = rtpParameters,
            Type          = producer.Data.Type
        };

        var score = new Antelcat.MediasoupSharp.FBS.Consumer.ConsumerScoreT
        {
            Score          = 10,
            ProducerScore  = 10,
            ProducerScores = []
        };

        var consumer = new ConsumerImpl<TConsumerAppData>(
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

        await ConsumersLock.WaitAsync();
        try
        {
            Consumers[consumer.Id] = consumer;
        }
        finally
        {
            ConsumersLock.Set();
        }
        
        consumer.On(static x => x.close,
            async () =>
            {
                await ConsumersLock.WaitAsync();
                try
                {
                    Consumers.Remove(consumer.Id);
                }
                finally
                {
                    ConsumersLock.Set();
                }
            }
        );
        consumer.On(static x => x.producerClose,
            async () =>
            {
                await ConsumersLock.WaitAsync();
                try
                {
                    Consumers.Remove(consumer.Id);
                }
                finally
                {
                    ConsumersLock.Set();
                }
            }
        );

        // Emit observer event.
        Observer.SafeEmit(static x => x.NewConsumer, consumer);

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

                this.SafeEmit(static x => x.SctpStateChange, Data.SctpState);

                // Emit observer event.
                Observer.SafeEmit(static x => x.SctpStateChange, Data.SctpState);

                break;
            }
            case Event.TRANSPORT_TRACE:
            {
                var traceNotification = notification.BodyAsTransport_TraceNotification().UnPack();

                this.SafeEmit(static x => x.Trace, traceNotification);

                // Emit observer event.
                Observer.SafeEmit(static x => x.Trace, traceNotification);

                break;
            }
            default:
            {
                logger.LogError($"{nameof(OnNotificationHandle)}() | PipeTransport:{{TransportId}} Ignoring unknown event:{{@event}}",
                    Id, @event);
                break;
            }
        }
    }

    private void HandleListenerError() {
        this.On(x=>x.ListenerError, tuple =>
        {
            logger.LogError(tuple.error,
                "event listener threw an error [eventName:{EventName}]:",
                tuple.eventName);
        });
    }
    #endregion Event Handlers
}