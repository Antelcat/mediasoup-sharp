using Antelcat.MediasoupSharp.Channel;
using Antelcat.MediasoupSharp.Consumer;
using Antelcat.MediasoupSharp.EnhancedEvent;
using Antelcat.MediasoupSharp.RtpParameters;
using Antelcat.MediasoupSharp.Transport;
using FBS.Notification;
using FBS.PipeTransport;
using FBS.Request;
using FBS.Transport;
using Antelcat.MediasoupSharp.Internals.Extensions;
using Antelcat.MediasoupSharp.RtpParameters.Extensions;
using Microsoft.Extensions.Logging;

namespace Antelcat.MediasoupSharp.PipeTransport;

public class PipeTransport : Transport.Transport
{
    /// <summary>
    /// Logger.
    /// </summary>
    private readonly ILogger<PipeTransport> logger = new Logger.Logger<PipeTransport>();

    /// <summary>
    /// PipeTransport data.
    /// </summary>
    public PipeTransportData Data { get; }

    /// <summary>
    /// <para>Events:</para>
    /// <para>@emits sctpstatechange - (sctpState: SctpState)</para>
    /// <para>@emits trace - (trace: TransportTraceEventData)</para>
    /// <para>Observer events:</para>
    /// <para>@emits close</para>
    /// <para>@emits newproducer - (producer: Producer)</para>
    /// <para>@emits newconsumer - (consumer: Consumer)</para>
    /// <para>@emits newdataproducer - (dataProducer: DataProducer)</para>
    /// <para>@emits newdataconsumer - (dataConsumer: DataConsumer)</para>
    /// <para>@emits sctpstatechange - (sctpState: SctpState)</para>
    /// <para>@emits trace - (trace: TransportTraceEventData)</para>
    /// </summary>
    public PipeTransport(
        TransportInternal @internal,
        PipeTransportData data,
        IChannel channel,
        AppData? appData,
        Func<RtpCapabilities> getRouterRtpCapabilities,
        Func<string, Task<Producer.Producer?>> getProducerById,
        Func<string, Task<DataProducer.DataProducer?>> getDataProducerById
    )
        : base(
            @internal,
            data,
            channel,
            appData,
            getRouterRtpCapabilities,
            getProducerById,
            getDataProducerById,
            new EnhancedEventEmitter()
        )
    {
        Data = data;

        HandleWorkerNotifications();
    }

    /// <summary>
    /// Close the PipeTransport.
    /// </summary>
    protected override Task OnCloseAsync()
    {
        if(Data.SctpState.HasValue)
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

        var response = await Channel.RequestAsync(bufferBuilder, Method.TRANSPORT_DUMP, null, null, Internal.TransportId);
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

        var response = await Channel.RequestAsync(bufferBuilder, Method.TRANSPORT_GET_STATS, null, null, Internal.TransportId);
        var data = response.Value.BodyAsPipeTransport_GetStatsResponse().UnPack();

        return [data];
    }

    /// <summary>
    /// Provide the PipeTransport remote parameters.
    /// </summary>
    protected override async Task OnConnectAsync(object parameters)
    {
        logger.LogDebug("OnConnectAsync() | PipeTransport:{TransportId}", Id);

        if(parameters is not ConnectRequestT connectRequestT)
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
    public override async Task<Consumer.Consumer> ConsumeAsync(ConsumerOptions consumerOptions)
    {
        logger.LogDebug("ConsumeAsync()");

        if(consumerOptions.ProducerId.IsNullOrWhiteSpace())
        {
            throw new Exception("missing producerId");
        }

        var producer = await GetProducerById(consumerOptions.ProducerId) ?? throw new Exception($"Producer with id {consumerOptions.ProducerId} not found");

        // This may throw.
        var rtpParameters = ORTC.Ortc.GetPipeConsumerRtpParameters(producer.Data.ConsumableRtpParameters, Data.Rtx);

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

        var consumer = new Consumer.Consumer(
            new ConsumerInternal(Internal.RouterId, Internal.TransportId, consumerId),
            consumerData,
            Channel,
            AppData,
            responseData.Paused,
            responseData.ProducerPaused,
            score, // Not `responseData.Score`
            responseData.PreferredLayers
        );

        consumer.On(
            "@close",
            async _ =>
            {
                await ConsumersLock.WaitAsync();
                try
                {
                    Consumers.Remove(consumer.Id);
                }
                catch(Exception ex)
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
            async _ =>
            {
                await ConsumersLock.WaitAsync();
                try
                {
                    Consumers.Remove(consumer.Id);
                }
                catch(Exception ex)
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
        catch(Exception ex)
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
        if(handlerId != Internal.TransportId)
        {
            return;
        }

        switch(@event)
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
                logger.LogError("OnNotificationHandle() | PipeTransport:{TransportId} Ignoring unknown event:{@event}", Id, @event);
                break;
            }
        }
    }

    #endregion Event Handlers
}