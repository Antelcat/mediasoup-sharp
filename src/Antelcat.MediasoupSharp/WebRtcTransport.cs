using System.Reflection;
using Antelcat.AutoGen.ComponentModel;
using Antelcat.AutoGen.ComponentModel.Diagnostic;
using Antelcat.MediasoupSharp.Internals.Extensions;
using FBS.Notification;
using FBS.Request;
using FBS.SctpAssociation;
using FBS.Transport;
using FBS.WebRtcTransport;
using Microsoft.Extensions.Logging;
using TransportObserver =
    Antelcat.MediasoupSharp.IEnhancedEventEmitter<Antelcat.MediasoupSharp.TransportObserverEvents>;

namespace Antelcat.MediasoupSharp;

using WebRtcTransportObserver = EnhancedEventEmitter<WebRtcTransportObserverEvents>;

[Serializable]
[AutoDeconstruct]
public partial record WebRtcTransportOptions<TWebRtcTransportAppData> 
    : WebRtcTransportOptionsBase<TWebRtcTransportAppData>
{
    /// <summary>
    /// Instance of WebRtcServer. Mandatory unless listenIps is given.
    /// </summary>
    public IWebRtcServer? WebRtcServer { get; set; }

    /// <summary>
    /// Listening IP address or addresses in order of preference (first one is the
    /// preferred one).
    /// </summary>
    public FBS.Transport.ListenInfoT[]? ListenInfos { get; set; }
}

public record WebRtcTransportOptionsBase<TWebRtcTransportAppData>
{
    /// <summary>
    /// Listen in UDP. Default true.
    /// </summary>
    public bool? EnableUdp { get; set; } = true;

    /// <summary>
    /// Listen in TCP. Default false.
    /// </summary>
    public bool? EnableTcp { get; set; }

    /// <summary>
    /// Prefer UDP. Default false.
    /// </summary>
    public bool PreferUdp { get; set; }

    /// <summary>
    /// Prefer TCP. Default false.
    /// </summary>
    public bool PreferTcp { get; set; }

    /// <summary>
    /// Initial available outgoing bitrate (in bps). Default 600000.
    /// </summary>
    public uint InitialAvailableOutgoingBitrate { get; set; } = 600000;

    /// <summary>
    /// Create a SCTP association. Default false.
    /// </summary>
    public bool EnableSctp { get; set; }

    /// <summary>
    /// SCTP streams number.
    /// </summary>
    public FBS.SctpParameters.NumSctpStreamsT? NumSctpStreams { get; set; } = new() { Os = 1024, Mis = 1024 };

    /// <summary>
    /// Maximum allowed size for SCTP messages sent by DataProducers.
    /// Default 262144.
    /// </summary>
    public uint MaxSctpMessageSize { get; init; } = 262144;

    /// <summary>
    /// Maximum SCTP send buffer used by DataConsumers.
    /// Default 262144.
    /// </summary>
    public uint SctpSendBufferSize { get; set; } = 262144;

    /// <summary>
    /// ICE consent timeout (in seconds). If 0 it is disabled. Default 30.
    /// </summary>
    public byte IceConsentTimeout { get; set; } = 30;

    /// <summary>
    /// Custom application data.
    /// </summary>
    public TWebRtcTransportAppData? AppData { get; set; }
}

public class WebRtcTransportListenServer
{
    /// <summary>
    /// Instance of WebRtcServer. Mandatory unless listenIps is given.
    /// </summary>
    public required IWebRtcServer WebRtcServer { get; set; }
}

public class WebRtcTransportListenIndividual
{
    /// <summary>
    /// Listening IP address or addresses in order of preference (first one is the
    /// preferred one).
    /// </summary>
    public ListenInfoT[]? ListenInfos { get; set; }

    /// <summary>
    /// Fixed port to listen on instead of selecting automatically from Worker's port
    /// range.
    /// </summary>
    public ushort? Port { get; set; } = 0; // mediasoup-work needs >= 0
}

public abstract class WebRtcTransportEvents : TransportEvents
{
    public required IceState  IceStateChange;
    public required TupleT    IceSelectedTupleChange;
    public required DtlsState DtlsStateChange;
    public required SctpState SctpStateChange;
}

public abstract class WebRtcTransportObserverEvents : TransportObserverEvents
{
    public required IceState  IceStateChange;
    public required TupleT    IceSelectedTupleChange;
    public required DtlsState DtlsStateChange;
    public required SctpState SctpStateChange;
}

[AutoMetadataFrom(typeof(WebRtcTransportData), MemberTypes.Property,
    Leading =
        $"public static implicit operator {nameof(WebRtcTransportData)}(global::{nameof(FBS)}.{nameof(FBS.WebRtcTransport)}.{nameof(DumpResponseT)} source) => new (source.Base){{",
    Template = "{Name} = source.{Name},",
    Trailing = "};")]
[AutoMetadataFrom(typeof(WebRtcTransportData), MemberTypes.Property,
    Leading =
        $"public static implicit operator global::{nameof(FBS)}.{nameof(FBS.WebRtcTransport)}.{nameof(DumpResponseT)}({nameof(WebRtcTransportData)} source) => new (){{",
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

[AutoExtractInterface(Interfaces = [typeof(ITransport), typeof(IEnhancedEventEmitter<WebRtcTransportEvents>)])]
public class WebRtcTransport<TWebRtcTransportAppData> :
    Transport<TWebRtcTransportAppData, WebRtcTransportEvents, WebRtcTransportObserver>, IWebRtcTransport
    where TWebRtcTransportAppData : new()
{
    /// <summary>
    /// Logger.
    /// </summary>
    private readonly ILogger logger = new Logger<WebRtcTransport<TWebRtcTransportAppData>>();

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
    public WebRtcTransport(WebRtcTransportConstructorOptions<TWebRtcTransportAppData> options)
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
        logger.LogDebug("OnConnectAsync() | WebRtcTransportId:{WebRtcTransportId}", Id);

        if (parameters is not ConnectRequestT connectRequestT)
        {
            throw new Exception($"{nameof(parameters)} type is not FBS.WebRtcTransport.ConnectRequestT");
        }

        // Build Request
        var bufferBuilder = Channel.BufferPool.Get();

        var connectRequestOffset = ConnectRequest.Pack(bufferBuilder, connectRequestT);

        var response = await Channel.RequestAsync(bufferBuilder, Method.WEBRTCTRANSPORT_CONNECT,
            FBS.Request.Body.WebRtcTransport_ConnectRequest,
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
    public async Task<FBS.WebRtcTransport.IceParametersT> RestartIceAsync()
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

                this.Emit(static x => x.IceStateChange, Data.IceState);

                // Emit observer event.
                Observer.Emit(static x => x.IceStateChange, Data.IceState);

                break;
            }
            case Event.WEBRTCTRANSPORT_ICE_SELECTED_TUPLE_CHANGE:
            {
                var iceSelectedTupleChangeNotification =
                    notification.BodyAsWebRtcTransport_IceSelectedTupleChangeNotification().UnPack();

                Data.IceSelectedTuple = iceSelectedTupleChangeNotification.Tuple;

                this.Emit(static x => x.IceSelectedTupleChange, Data.IceSelectedTuple);

                // Emit observer event.
                Observer.Emit(static x => x.IceSelectedTupleChange, Data.IceSelectedTuple);

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

                this.Emit(static x => x.DtlsStateChange, Data.DtlsState);

                // Emit observer event.
                Observer.Emit(static x => x.DtlsStateChange, Data.DtlsState);

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

                this.Emit(static x => x.Trace, traceNotification);

                // Emit observer event.
                Observer.Emit(static x => x.Trace, traceNotification);

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