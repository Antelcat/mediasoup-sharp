using System.Reflection;
using Antelcat.AutoGen.ComponentModel;
using Antelcat.AutoGen.ComponentModel.Diagnostic;
using Antelcat.MediasoupSharp.Internals.Extensions;
using Antelcat.MediasoupSharp.FBS.Notification;
using Antelcat.MediasoupSharp.FBS.Request;
using Antelcat.MediasoupSharp.FBS.SctpAssociation;
using Antelcat.MediasoupSharp.FBS.Transport;
using Antelcat.MediasoupSharp.FBS.WebRtcTransport;
using Microsoft.Extensions.Logging;

namespace Antelcat.MediasoupSharp;

[AutoMetadataFrom(typeof(WebRtcTransportData), MemberTypes.Property,
    Leading =
        $"public static implicit operator {nameof(WebRtcTransportData)}(global::Antelcat.MediasoupSharp.FBS.WebRtcTransport.{nameof(DumpResponseT)} source) => new (source.Base){{",
    Template = "{Name} = source.{Name},",
    Trailing = "};")]
[AutoMetadataFrom(typeof(WebRtcTransportData), MemberTypes.Property,
    Leading =
        $"public static implicit operator global::Antelcat.MediasoupSharp.FBS.WebRtcTransport.{nameof(DumpResponseT)}({nameof(WebRtcTransportData)} source) => new (){{",
    Template = "{Name} = source.{Name},",
    Trailing = "Base = source  };")]
public partial class WebRtcTransportData(DumpT dump) : TransportBaseData(dump)
{
    public IceRole IceRole { get; init; }

    public required IceParametersT IceParameters { get; set; }

    public required List<IceCandidateT> IceCandidates { get; init; }

    public IceState IceState { get; set; }

    public TupleT? IceSelectedTuple { get; set; }

    public required DtlsParametersT DtlsParameters { get; init; }

    public DtlsState DtlsState { get; set; }

    public string? DtlsRemoteCert;
}

public class WebRtcTransportConstructorOptions<TWebRtcTransportAppData>(WebRtcTransportData data)
    : TransportConstructorOptions<TWebRtcTransportAppData>(data)
{
    public override WebRtcTransportData Data => base.Data.Sure<WebRtcTransportData>();
}

[AutoExtractInterface(
    NamingTemplate = nameof(IWebRtcTransport),
    Interfaces = [typeof(ITransport), typeof(IEnhancedEventEmitter<WebRtcTransportEvents>)])]
public class WebRtcTransportImpl<TWebRtcTransportAppData> :
    TransportImpl<
        TWebRtcTransportAppData,
        WebRtcTransportEvents,
        WebRtcTransportObserver
    >, IWebRtcTransport<TWebRtcTransportAppData>
    where TWebRtcTransportAppData : new()
{
    /// <summary>
    /// Logger.
    /// </summary>
    private readonly ILogger logger = new Logger<IWebRtcTransport>();

    public override WebRtcTransportData Data { get; }

    public override WebRtcTransportObserver Observer => base.Observer.Sure<WebRtcTransportObserver>();

    /// <summary>
    /// <para>Events:</para>
    /// <para>@emits <see cref="WebRtcTransportEvents.IceStateChange"/> - (iceState: IceState)</para>
    /// <para>@emits <see cref="WebRtcTransportEvents.IceSelectedTupleChange"/> - (iceSelectedTuple: TransportTuple)</para>
    /// <para>@emits <see cref="WebRtcTransportEvents.DtlsStateChange"/> - (dtlsState: DtlsState)</para>
    /// <para>@emits <see cref="WebRtcTransportEvents.SctpStateChange"/> - (sctpState: SctpState)</para>
    /// <para>@emits <see cref="TransportEvents.Trace"/> - (trace: TransportTraceEventData)</para>
    /// <para>Observer events:</para>
    /// <para>@emits <see cref="TransportObserverEvents.Close"/></para>
    /// <para>@emits <see cref="TransportObserverEvents.NewProducer"/> - (producer: Producer)</para>
    /// <para>@emits <see cref="TransportObserverEvents.NewConsumer"/> - (consumer: Consumer)</para>
    /// <para>@emits <see cref="TransportObserverEvents.NewDataProducer"/> - (dataProducer: DataProducer)</para>
    /// <para>@emits <see cref="TransportObserverEvents.NewDataConsumer"/> - (dataConsumer: DataConsumer)</para>
    /// <para>@emits <see cref="WebRtcTransportObserverEvents.IceStateChange"/> - (iceState: IceState)</para>
    /// <para>@emits <see cref="WebRtcTransportObserverEvents.IceSelectedTupleChange"/> - (iceSelectedTuple: TransportTuple)</para>
    /// <para>@emits <see cref="WebRtcTransportObserverEvents.DtlsStateChange"/> - (dtlsState: DtlsState)</para>
    /// <para>@emits <see cref="WebRtcTransportObserverEvents.SctpStateChange"/> - (sctpState: SctpState)</para>
    /// <para>@emits <see cref="TransportObserverEvents.Trace"/> - (trace: TransportTraceEventData)</para>
    /// </summary>
    public WebRtcTransportImpl(WebRtcTransportConstructorOptions<TWebRtcTransportAppData> options)
        : base(options, new WebRtcTransportObserver())
    {
        Data = options.Data;

        HandleWorkerNotifications();
    }

    /// <summary>
    /// Close the WebRtcTransport.
    /// </summary>
    protected override Task OnCloseAsync()
    {
        Data.IceState         = IceState.DISCONNECTED; // CLOSED
        Data.IceSelectedTuple = null;
        Data.DtlsState        = DtlsState.CLOSED;

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
        var data = response.NotNull().BodyAsWebRtcTransport_DumpResponse().UnPack();

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
        var data = response.NotNull().BodyAsWebRtcTransport_GetStatsResponse().UnPack();

        return [data];
    }

    /// <summary>
    /// Provide the WebRtcTransport remote parameters.
    /// </summary>
    protected override async Task OnConnectAsync(object parameters)
    {
        logger.LogDebug($"{nameof(OnConnectAsync)}() | WebRtcTransportId:{{WebRtcTransportId}}", Id);

        if (parameters is not ConnectRequestT connectRequestT)
        {
            throw new Exception($"{nameof(parameters)} type is not Antelcat.MediasoupSharp.FBS.WebRtcTransport.ConnectRequestT");
        }

        // Build Request
        var bufferBuilder = Channel.BufferPool.Get();

        var connectRequestOffset = ConnectRequest.Pack(bufferBuilder, connectRequestT);

        var response = await Channel.RequestAsync(bufferBuilder, Method.WEBRTCTRANSPORT_CONNECT,
            Antelcat.MediasoupSharp.FBS.Request.Body.WebRtcTransport_ConnectRequest,
            connectRequestOffset.Value,
            Internal.TransportId);

        /* Decode Response. */
        var data = response.NotNull().BodyAsWebRtcTransport_ConnectResponse().UnPack();

        // Update data.
        Data.DtlsParameters.Role = data.DtlsLocalRole;
    }

    /// <summary>
    /// Restart ICE.
    /// </summary>
    public async Task<Antelcat.MediasoupSharp.FBS.WebRtcTransport.IceParametersT> RestartIceAsync()
    {
        logger.LogDebug("RestartIceAsync() | WebRtcTransportId:{WebRtcTransportId}", Id);

        await using (await CloseLock.ReadLockAsync())
        {
            if (Closed)
            {
                throw new InvalidStateException("Transport closed");
            }

            // Build Request
            var bufferBuilder = Channel.BufferPool.Get();

            var response = await Channel.RequestAsync(bufferBuilder, Method.TRANSPORT_RESTART_ICE,
                null,
                null,
                Internal.TransportId);

            /* Decode Response. */
            var data = response.NotNull().BodyAsTransport_RestartIceResponse().UnPack();

            // Update data.
            Data.IceParameters = new IceParametersT
            {
                UsernameFragment = data.UsernameFragment,
                Password         = data.Password,
                IceLite          = data.IceLite
            };

            return Data.IceParameters;
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
            case Event.WEBRTCTRANSPORT_ICE_STATE_CHANGE:
            {
                var iceStateChangeNotification =
                    notification.BodyAsWebRtcTransport_IceStateChangeNotification().UnPack();

                Data.IceState = iceStateChangeNotification.IceState;

                this.SafeEmit(static x => x.IceStateChange, Data.IceState);

                // Emit observer event.
                Observer.SafeEmit(static x => x.IceStateChange, Data.IceState);

                break;
            }
            case Event.WEBRTCTRANSPORT_ICE_SELECTED_TUPLE_CHANGE:
            {
                var iceSelectedTupleChangeNotification =
                    notification.BodyAsWebRtcTransport_IceSelectedTupleChangeNotification().UnPack();

                Data.IceSelectedTuple = iceSelectedTupleChangeNotification.Tuple;

                this.SafeEmit(static x => x.IceSelectedTupleChange, Data.IceSelectedTuple);

                // Emit observer event.
                Observer.SafeEmit(static x => x.IceSelectedTupleChange, Data.IceSelectedTuple);

                break;
            }

            case Event.WEBRTCTRANSPORT_DTLS_STATE_CHANGE:
            {
                var dtlsStateChangeNotification =
                    notification.BodyAsWebRtcTransport_DtlsStateChangeNotification().UnPack();

                Data.DtlsState = dtlsStateChangeNotification.DtlsState;

                if (Data.DtlsState == DtlsState.CONNECTED)
                {
                    // TODO: DtlsRemoteCert do not exists.
                    // Data.DtlsRemoteCert = dtlsStateChangeNotification.RemoteCert;
                }

                this.SafeEmit(static x => x.DtlsStateChange, Data.DtlsState);

                // Emit observer event.
                Observer.SafeEmit(static x => x.DtlsStateChange, Data.DtlsState);

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

                this.SafeEmit(static x => x.Trace, traceNotification);

                // Emit observer event.
                Observer.SafeEmit(static x => x.Trace, traceNotification);

                break;
            }
            default:
            {
                logger.LogError(
                    "OnNotificationHandle() | WebRtcTransport:{TransportId} Ignoring unknown event:{Event}", Id,
                    @event);
                break;
            }
        }
    }

    #endregion Event Handlers
}