﻿using Antelcat.AutoGen.ComponentModel.Diagnostic;
using Antelcat.MediasoupSharp.Internals.Extensions;
using FBS.Notification;
using FBS.Request;
using FBS.Transport;
using Microsoft.Extensions.Logging;

namespace Antelcat.MediasoupSharp;

using DirectTransportObserver = EnhancedEventEmitter<DirectTransportObserverEvents>;

public class DirectTransportOptions<TDirectTransportAppData>
{
    /// <summary>
    /// Maximum allowed size for direct messages sent from DataProducers.
    /// Default 262144.
    /// </summary>
    public uint MaxMessageSize { get; set; } = 262144;

    /// <summary>
    /// Custom application data.
    /// </summary>
    public TDirectTransportAppData? AppData { get; set; }
}

public class DirectTransportEvents : TransportEvents
{
    public List<byte> rtcp;
}

public class DirectTransportObserverEvents : TransportObserverEvents
{
    public List<byte> rtcp;
}

public class DirectTransportConstructorOptions<TDirectTransportAppData>(TransportBaseData data)
    : TransportConstructorOptions<TDirectTransportAppData>(data);


[AutoExtractInterface(Interfaces = [typeof(ITransport)])]
public class DirectTransport<TDirectTransportAppData>
    : Transport<TDirectTransportAppData, DirectTransportEvents, DirectTransportObserver>, IDirectTransport
    where TDirectTransportAppData : new()
{
    /// <summary>
    /// Logger.
    /// </summary>
    private readonly ILogger logger = new Logger<DirectTransport<TDirectTransportAppData>>();

    /// <summary>
    /// <para>Events:</para>
    /// <para>@emits rtcp - (packet: Buffer)</para>
    /// <para>@emits trace - (trace: TransportTraceEventData)</para>
    /// <para>Observer events:</para>
    /// <para>@emits close</para>
    /// <para>@emits newdataproducer - (dataProducer: DataProducer)</para>
    /// <para>@emits newdataconsumer - (dataProducer: DataProducer)</para>
    /// <para>@emits trace - (trace: TransportTraceEventData)</para>
    /// </summary>
    public DirectTransport(DirectTransportConstructorOptions<TDirectTransportAppData> options)
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
        var data = response.Value.BodyAsDirectTransport_DumpResponse().UnPack();

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
        var data = response.Value.BodyAsDirectTransport_GetStatsResponse().UnPack();
        return [data];
    }

    /// <summary>
    /// NO-OP method in DirectTransport.
    /// </summary>
    protected override Task OnConnectAsync(object parameters)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Set maximum incoming bitrate for receiving media.
    /// </summary>
    public override Task<string> SetMaxIncomingBitrateAsync(uint bitrate)
    {
        logger.LogError("SetMaxIncomingBitrateAsync() | DiectTransport:{TransportId} Bitrate:{bitrate}", Id,
            Id);
        throw new NotImplementedException("SetMaxIncomingBitrateAsync() not implemented in DirectTransport");
    }

    /// <summary>
    /// Set maximum outgoing bitrate for sending media.
    /// </summary>
    public override Task<string> SetMaxOutgoingBitrateAsync(uint bitrate)
    {
        logger.LogError("SetMaxOutgoingBitrateAsync() | DiectTransport:{TransportId} Bitrate:{bitrate}", Id,
            Id);
        throw new NotImplementedException("SetMaxOutgoingBitrateAsync is not implemented in DirectTransport");
    }

    /// <summary>
    /// Set minimum outgoing bitrate for sending media.
    /// </summary>
    public override Task<string> SetMinOutgoingBitrateAsync(uint bitrate)
    {
        logger.LogError("SetMinOutgoingBitrateAsync() | DiectTransport:{TransportId} Bitrate:{bitrate}", Id,
            Id);
        throw new NotImplementedException("SetMinOutgoingBitrateAsync is not implemented in DirectTransport");
    }

    /*/// <summary>
    /// Create a Producer.
    /// </summary>
    public override Task<Producer.Producer> ProduceAsync(ProducerOptions producerOptions)
    {
        logger.LogError("ProduceAsync() | DirectTransport:{TransportId}", Id);
        throw new NotImplementedException("ProduceAsync() is not implemented in DirectTransport");
    }*/


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

                this.Emit(static x=>x.trace, traceNotification);

                // Emit observer event.
                Observer.Emit(static x=>x.trace, traceNotification);

                break;
            }
            default:
            {
                logger.LogError(
                    "OnNotificationHandle() | DirectTransport:{TransportId} Ignoring unknown event:{@event}",
                    Id, @event);
                break;
            }
        }
    }

    #endregion Event Handlers
}