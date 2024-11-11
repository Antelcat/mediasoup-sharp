using Antelcat.AutoGen.ComponentModel.Diagnostic;
using Antelcat.MediasoupSharp.Internals.Extensions;
using FBS.Notification;
using FBS.Request;
using FBS.Transport;
using Microsoft.Extensions.Logging;

namespace Antelcat.MediasoupSharp;

public class DirectTransportConstructorOptions<TDirectTransportAppData>(TransportBaseData data)
    : TransportConstructorOptions<TDirectTransportAppData>(data);

[AutoExtractInterface(
    NamingTemplate = nameof(IDirectTransport),
    Interfaces = [typeof(ITransport)],
    Exclude = [nameof(ProduceAsync)])]
public class DirectTransportImpl<TDirectTransportAppData>
    : Transport<
            TDirectTransportAppData, 
            DirectTransportEvents, 
            DirectTransportObserver
        >, IDirectTransport<TDirectTransportAppData>
    where TDirectTransportAppData : new()
{
    /// <summary>
    /// Logger.
    /// </summary>
    private readonly ILogger logger = new Logger<IDirectTransport>();

    /// <summary>
    /// <para>Events:</para>
    /// <para>@emits <see cref="DirectTransportEvents.Rtcp"/> - (packet: Buffer)</para>
    /// <para>@emits <see cref="TransportEvents.Trace"/> - (trace: TransportTraceEventData)</para>
    /// <para>Observer events:</para>
    /// <para>@emits <see cref="TransportObserverEvents.Close"/></para>
    /// <para>@emits <see cref="TransportObserverEvents.NewDataProducer"/> - (dataProducer: DataProducer)</para>
    /// <para>@emits <see cref="TransportObserverEvents.NewDataConsumer"/> - (dataProducer: DataProducer)</para>
    /// <para>@emits <see cref="TransportObserverEvents.Trace"/> - (trace: TransportTraceEventData)</para>
    /// </summary>
    public DirectTransportImpl(DirectTransportConstructorOptions<TDirectTransportAppData> options)
        : base(options, new DirectTransportObserver())
    {
        HandleWorkerNotifications();
    }

    /// <summary>
    /// Close the DirectTransport.
    /// </summary>
    protected override Task OnCloseAsync() => Task.CompletedTask;

    /// <summary>
    /// Router was closed.
    /// </summary>
    protected override Task OnRouterClosedAsync() => Task.CompletedTask;

    /// <summary>
    /// Dump Transport.
    /// </summary>
    protected override async Task<object> OnDumpAsync()
    {
        // Build Request
        var bufferBuilder = Channel.BufferPool.Get();

        var response =
            await Channel.RequestAsync(bufferBuilder, Method.TRANSPORT_DUMP, null, null, Internal.TransportId);
        var data = response.NotNull().BodyAsDirectTransport_DumpResponse().UnPack();

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
        var data = response.NotNull().BodyAsDirectTransport_GetStatsResponse().UnPack();
        return [data];
    }

    /// <summary>
    /// NO-OP method in DirectTransport.
    /// </summary>
    protected override Task OnConnectAsync(object parameters) => Task.CompletedTask;

    /// <summary>
    /// Set maximum incoming bitrate for receiving media.
    /// </summary>
    public override Task<string> SetMaxIncomingBitrateAsync(uint bitrate)
    {
        logger.LogError($"{nameof(SetMaxIncomingBitrateAsync)}() | DirectTransport:{{TransportId}} Bitrate:{{Bitrate}}",
            Id, bitrate);
        throw new NotSupportedException($"{nameof(SetMaxIncomingBitrateAsync)}() not implemented in DirectTransport");
    }

    /// <summary>
    /// Set maximum outgoing bitrate for sending media.
    /// </summary>
    public override Task<string> SetMaxOutgoingBitrateAsync(uint bitrate)
    {
        logger.LogError($"{nameof(SetMaxOutgoingBitrateAsync)}() | DirectTransport:{{TransportId}} Bitrate:{{Bitrate}}",
            Id, bitrate);
        throw new NotSupportedException($"{nameof(SetMaxOutgoingBitrateAsync)} is not implemented in DirectTransport");
    }

    /// <summary>
    /// Set minimum outgoing bitrate for sending media.
    /// </summary>
    public override Task<string> SetMinOutgoingBitrateAsync(uint bitrate)
    {
        logger.LogError($"{nameof(SetMinOutgoingBitrateAsync)}() | DirectTransport:{{TransportId}} Bitrate:{{Bitrate}}",
            Id,
            Id);
        throw new NotSupportedException($"{nameof(SetMinOutgoingBitrateAsync)} is not implemented in DirectTransport");
    }

    public override Task<ProducerImpl<TProducerAppData>> ProduceAsync<TProducerAppData>(
        ProducerOptions<TProducerAppData> producerOptions)
    {
        logger.LogError($"{nameof(ProduceAsync)}() | DirectTransport:{{TransportId}}", Id);
        throw new NotSupportedException($"{nameof(ProduceAsync)}() is not implemented in DirectTransport");
    }


    public async Task SendRtcpAsync(byte[] rtcpPacket)
    {
        await using (await CloseLock.ReadLockAsync())
        {
            if (Closed)
            {
                throw new InvalidStateException("Transport closed");
            }

            // Build Request
            var bufferBuilder = Channel.BufferPool.Get();

            var sendRtcpNotification = new SendRtcpNotificationT
            {
                Data = rtcpPacket.ToList()
            };

            var sendRtcpNotificationOffset = SendRtcpNotification.Pack(bufferBuilder, sendRtcpNotification);

            // Fire and forget
            Channel.NotifyAsync(bufferBuilder, Event.TRANSPORT_SEND_RTCP,
                FBS.Notification.Body.Transport_SendRtcpNotification,
                sendRtcpNotificationOffset.Value,
                Internal.TransportId
            ).ContinueWithOnFaultedHandleLog(logger);
        }
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
            case Event.TRANSPORT_TRACE:
            {
                var traceNotification = notification.BodyAsTransport_TraceNotification().UnPack();

                this.Emit(static x => x.Trace, traceNotification);

                // Emit observer event.
                Observer.Emit(static x => x.Trace, traceNotification);

                break;
            }
            default:
            {
                logger.LogError(
                    $"{nameof(OnNotificationHandle)}() | DirectTransport:{{TransportId}} Ignoring unknown event:{{Event}}",
                    Id, @event);
                break;
            }
        }
    }

    #endregion Event Handlers
}