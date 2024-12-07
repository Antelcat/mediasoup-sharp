using System.Reflection;
using Antelcat.AutoGen.ComponentModel.Diagnostic;
using Antelcat.MediasoupSharp.FBS.Notification;
using Antelcat.MediasoupSharp.FBS.PlainTransport;
using Antelcat.MediasoupSharp.FBS.Request;
using Antelcat.MediasoupSharp.FBS.SrtpParameters;
using Antelcat.MediasoupSharp.FBS.Transport;
using Antelcat.MediasoupSharp.Internals.Extensions;
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
public partial record PlainTransportData : TransportBaseData
{
    public PlainTransportData(DumpT dump) : base(dump) { }
    
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
    /// <para>Events : <see cref="PlainTransportEvents"/></para>
    /// <para>Observer events : <see cref="TransportObserverEvents"/></para>
    /// </summary>
    public PlainTransportImpl(PlainTransportConstructorOptions<TPlainTransportAppData> options)
        : base(options, new PlainTransportObserver())
    {
        Data = options.Data with { };

        HandleWorkerNotifications();
        HandleListenerError();
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

                this.SafeEmit(static x => x.Tuple, Data.Tuple);

                // Emit observer event.
                Observer.SafeEmit(static x => x.Tuple, Data.Tuple);

                break;
            }
            case Event.PLAINTRANSPORT_RTCP_TUPLE:
            {
                var rtcpTupleNotification = notification.BodyAsPlainTransport_RtcpTupleNotification().UnPack();

                Data.RtcpTuple = rtcpTupleNotification.Tuple;

                this.SafeEmit(static x => x.RtcpTuple, Data.RtcpTuple);

                // Emit observer event.
                Observer.SafeEmit(static x => x.RtcpTuple, Data.RtcpTuple);

                break;
            }
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

                this.SafeEmit(static x=>x.Trace, traceNotification);

                // Emit observer event.
                Observer.SafeEmit(static x => x.Trace, traceNotification);

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

    private void HandleListenerError() =>
        this.On(static x => x.ListenerError, tuple =>
        {
            logger.LogError(tuple.error,
                "event listener threw an error [eventName:{EventName}]:",
                tuple.eventName);
        });

    #endregion Event Handlers
}