﻿using System.Reflection;
using Antelcat.AutoGen.ComponentModel.Diagnostic;
using Antelcat.MediasoupSharp.Internals.Extensions;
using Antelcat.MediasoupSharp.FBS.Notification;
using Antelcat.MediasoupSharp.FBS.PlainTransport;
using Antelcat.MediasoupSharp.FBS.Request;
using Antelcat.MediasoupSharp.FBS.SrtpParameters;
using Antelcat.MediasoupSharp.FBS.Transport;
using Microsoft.Extensions.Logging;

namespace Antelcat.MediasoupSharp;

public class PlainTransportConstructorOptions<TPlainTransportAppData>(PlainTransportData data)
    : TransportConstructorOptions<TPlainTransportAppData>(data)
{
    public override PlainTransportData Data => base.Data.Sure<PlainTransportData>();
}

[AutoMetadataFrom(typeof(PlainTransportData), MemberTypes.Property,
    Leading =
        $"public static implicit operator {nameof(PlainTransportData)}(global::Antelcat.MediasoupSharp.FBS.PlainTransport.{nameof(DumpResponseT)} source) => new (source.Base){{",
    Template = "{Name} = source.{Name},",
    Trailing = "};")]
[AutoMetadataFrom(typeof(PlainTransportData), MemberTypes.Property,
    Leading =
        $"public static implicit operator global::Antelcat.MediasoupSharp.FBS.PlainTransport.{nameof(DumpResponseT)}({nameof(PlainTransportData)} source) => new (){{",
    Template = "{Name} = source.{Name},",
    Trailing = "Base = source  };")]
public partial class PlainTransportData(DumpT dump) : TransportBaseData(dump)
{
    public bool RtcpMux { get; set; }

    public bool Comedia { get; set; }

    public required TupleT Tuple { get; set; }

    public TupleT? RtcpTuple { get; set; }

    public SrtpParametersT? SrtpParameters { get; set; }
}

[AutoExtractInterface(
    NamingTemplate = nameof(IPlainTransport),
    Interfaces = [typeof(ITransport), typeof(IEnhancedEventEmitter<PlainTransportEvents>)])]
public class PlainTransportImpl<TPlainTransportAppData>
    : TransportImpl<
            TPlainTransportAppData, 
            PlainTransportEvents, 
            PlainTransportObserver
        >, IPlainTransport<TPlainTransportAppData>
    where TPlainTransportAppData : new()
{
    /// <summary>
    /// Logger.
    /// </summary>
    private readonly ILogger logger = new Logger<IPlainTransport>();

    /// <summary>
    /// Producer data.
    /// </summary>
    public override PlainTransportData Data { get; }

    public override PlainTransportObserver Observer => base.Observer.Sure<PlainTransportObserver>();

    /// <summary>
    /// <para>Events:</para>
    /// <para>@emits tuple - (tuple: TransportTuple)</para>
    /// <para>@emits <see cref="PlainTransportEvents.RtcpTuple"/> - (rtcpTuple: TransportTuple)</para>
    /// <para>@emits <see cref="PlainTransportEvents.SctpStateChange"/> - (sctpState: SctpState)</para>
    /// <para>@emits trace - (trace: TransportTraceEventData)</para>
    /// <para>Observer events:</para>
    /// <para>@emits close</para>
    /// <para>@emits <see cref="TransportObserverEvents.NewProducer"/> - (producer: Producer)</para>
    /// <para>@emits <see cref="TransportObserverEvents.NewConsumer"/> - (consumer: Consumer)</para>
    /// <para>@emits <see cref="TransportObserverEvents.NewDataProducer"/> - (dataProducer: DataProducer)</para>
    /// <para>@emits <see cref="TransportObserverEvents.NewDataConsumer"/> - (dataConsumer: DataConsumer)</para>
    /// <para>@emits <see cref="PlainTransportObserverEvents.Tuple"/> - (tuple: TransportTuple)</para>
    /// <para>@emits <see cref="PlainTransportObserverEvents.RtcpTuple"/> - (rtcpTuple: TransportTuple)</para>
    /// <para>@emits <see cref="PlainTransportObserverEvents.SctpStateChange"/> - (sctpState: SctpState)</para>
    /// <para>@emits <see cref="TransportObserverEvents.Trace"/> - (trace: TransportTraceEventData)</para>
    /// </summary>
    public PlainTransportImpl(PlainTransportConstructorOptions<TPlainTransportAppData> options)
        : base(options, new PlainTransportObserver())
    {
        Data = options.Data;

        HandleWorkerNotifications();
    }

    /// <summary>
    /// Close the PlainTransport.
    /// </summary>
    protected override Task OnCloseAsync()
    {
        if (Data.SctpState.HasValue)
        {
            Data.SctpState = Antelcat.MediasoupSharp.FBS.SctpAssociation.SctpState.CLOSED;
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
        var data = response.NotNull().BodyAsPlainTransport_DumpResponse().UnPack();

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
        var data = response.NotNull().BodyAsPlainTransport_GetStatsResponse().UnPack();

        return [data];
    }

    /// <summary>
    /// Provide the PlainTransport remote parameters.
    /// </summary>
    protected override async Task OnConnectAsync(object parameters)
    {
        logger.LogDebug("OnConnectAsync() | PlainTransport:{TransportId}", Id);

        if (parameters is not ConnectRequestT connectRequestT)
        {
            throw new Exception($"{nameof(parameters)} type is not Antelcat.MediasoupSharp.FBS.PlainTransport.ConnectRequestT");
        }

        // Build Request
        var bufferBuilder = Channel.BufferPool.Get();

        var connectRequestOffset = ConnectRequest.Pack(bufferBuilder, connectRequestT);

        var response = await Channel.RequestAsync(bufferBuilder, Method.PLAINTRANSPORT_CONNECT,
            Antelcat.MediasoupSharp.FBS.Request.Body.PlainTransport_ConnectRequest,
            connectRequestOffset.Value,
            Internal.TransportId);

        /* Decode Response. */
        var data = response.NotNull().BodyAsPlainTransport_ConnectResponse().UnPack();

        // Update data.
        if (data.Tuple != null)
        {
            Data.Tuple = data.Tuple;
        }

        if (data.RtcpTuple != null)
        {
            Data.RtcpTuple = data.RtcpTuple;
        }

        Data.SrtpParameters = data.SrtpParameters;
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
            case Event.PLAINTRANSPORT_TUPLE:
            {
                var tupleNotification = notification.BodyAsPlainTransport_TupleNotification().UnPack();

                Data.Tuple = tupleNotification.Tuple;

                this.Emit(static x => x.Tuple, Data.Tuple);

                // Emit observer event.
                Observer.Emit(static x => x.Tuple, Data.Tuple);

                break;
            }
            case Event.PLAINTRANSPORT_RTCP_TUPLE:
            {
                var rtcpTupleNotification = notification.BodyAsPlainTransport_RtcpTupleNotification().UnPack();

                Data.RtcpTuple = rtcpTupleNotification.Tuple;

                this.Emit(static x => x.RtcpTuple, Data.RtcpTuple);

                // Emit observer event.
                Observer.Emit(static x => x.RtcpTuple, Data.RtcpTuple);

                break;
            }
            case Event.TRANSPORT_SCTP_STATE_CHANGE:
            {
                var sctpStateChangeNotification = notification.BodyAsTransport_SctpStateChangeNotification().UnPack();

                Data.SctpState = sctpStateChangeNotification.SctpState;

                this.Emit(static x => x.SctpStateChange, Data.SctpState);

                // Emit observer event.
                Observer.Emit(static x => x.SctpStateChange, Data.SctpState);

                break;
            }
            case Event.TRANSPORT_TRACE:
            {
                var traceNotification = notification.BodyAsTransport_TraceNotification().UnPack();

                this.Emit(static x=>x.Trace, traceNotification);

                // Emit observer event.
                Observer.Emit(static x => x.Trace, traceNotification);

                break;
            }
            default:
            {
                logger.LogError($"{nameof(OnNotificationHandle)}() | PlainTransport:{{TransportId}} Ignoring unknown event:{{@event}}",
                    Id, @event);
                break;
            }
        }
    }

    #endregion Event Handlers
}