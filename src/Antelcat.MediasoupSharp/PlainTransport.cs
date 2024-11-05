using System.Linq.Expressions;
using System.Reflection;
using Antelcat.AutoGen.ComponentModel.Diagnostic;
using Antelcat.MediasoupSharp.Internals.Extensions;
using FBS.Notification;
using FBS.PlainTransport;
using FBS.Request;
using FBS.SctpAssociation;
using FBS.SctpParameters;
using FBS.SrtpParameters;
using FBS.Transport;
using Microsoft.Extensions.Logging;
using TransportObserver = Antelcat.MediasoupSharp.IEnhancedEventEmitter<Antelcat.MediasoupSharp.TransportObserverEvents>;

namespace Antelcat.MediasoupSharp;

using PlainTransportObserver = EnhancedEventEmitter<PlainTransportObserverEvents>;

public record PlainTransportOptions<TPlainTransportAppData>
{
    /// <summary>
    /// Listening information.
    /// </summary>
    public required ListenInfoT ListenInfo { get; set; }

    /// <summary>
    /// RTCP listening information. If not given and rtcpPort is not false,
    /// RTCP will use same listening info than RTP.
    /// </summary>
    public ListenInfoT? RtcpListenInfo { get; set; }

    /// <summary>
    /// Use RTCP-mux (RTP and RTCP in the same port). Default true.
    /// </summary>
    public bool? RtcpMux { get; set; } = true;

    /// <summary>
    /// Whether remote IP:port should be auto-detected based on first RTP/RTCP
    /// packet received. If enabled, connect() method must not be called unless
    /// SRTP is enabled. If so, it must be called with just remote SRTP parameters.
    /// Default false.
    /// </summary>
    public bool? Comedia { get; set; } = false;

    /// <summary>
    /// Create a SCTP association. Default false.
    /// </summary>
    public bool? EnableSctp { get; set; } = false;

    /// <summary>
    /// SCTP streams number.
    /// </summary>
    public NumSctpStreamsT? NumSctpStreams { get; set; }

    /// <summary>
    /// Maximum allowed size for SCTP messages sent by DataProducers.
    /// Default 262144.
    /// </summary>
    public uint? MaxSctpMessageSize { get; set; } = 262144;

    /// <summary>
    /// Maximum SCTP send buffer used by DataConsumers.
    /// Default 262144.
    /// </summary>
    public uint? SctpSendBufferSize { get; set; } = 262144;

    /// <summary>
    /// Enable SRTP. For this to work, connect() must be called
    /// with remote SRTP parameters. Default false.
    /// </summary>
    public bool? EnableSrtp { get; set; } = false;

    /// <summary>
    /// The SRTP crypto suite to be used if enableSrtp is set. Default
    /// 'AES_CM_128_HMAC_SHA1_80'.
    /// </summary>
    public SrtpCryptoSuite? SrtpCryptoSuite { get; set; } = FBS.SrtpParameters.SrtpCryptoSuite.AES_CM_128_HMAC_SHA1_80;

    /// <summary>
    /// Custom application data.
    /// </summary>
    public TPlainTransportAppData? AppData { get; set; }
}

public class PlainTransportEvents : TransportEvents
{
    public TupleT    tuple;
    public TupleT    rtcptuple;
    public SctpState sctpstatechange;
}

public class PlainTransportObserverEvents : TransportObserverEvents
{
    public TupleT    tuple;
    public TupleT    rtcptuple;
    public SctpState sctpstatechange;
}

public class PlainTransportConstructorOptions<TPlainTransportAppData>(PlainTransportData data)
    : TransportConstructorOptions<TPlainTransportAppData>(data)
{
    public override PlainTransportData Data => base.Data.Sure<PlainTransportData>();
}

[AutoMetadataFrom(typeof(PlainTransportData), MemberTypes.Property,
    Leading =
        $"public static implicit operator {nameof(PlainTransportData)}(global::{nameof(FBS)}.{nameof(FBS.PlainTransport)}.{nameof(DumpResponseT)} source) => new (source.Base){{",
    Template = "{Name} = source.{Name},",
    Trailing = "};")]
[AutoMetadataFrom(typeof(PlainTransportData), MemberTypes.Property,
    Leading =
        $"public static implicit operator global::{nameof(FBS)}.{nameof(FBS.PlainTransport)}.{nameof(DumpResponseT)}({nameof(PlainTransportData)} source) => new (){{",
    Template = "{Name} = source.{Name},",
    Trailing = "Base = source  };")]
public partial class PlainTransportData(DumpT dump) : TransportBaseData(dump)
{
    public bool RtcpMux { get; set; }

    public bool Comedia { get; set; }

    public TupleT Tuple { get; set; }

    public TupleT? RtcpTuple { get; set; }

    public SrtpParametersT? SrtpParameters { get; set; }
}

[AutoExtractInterface(Interfaces = [typeof(ITransport), typeof(IEnhancedEventEmitter<PlainTransportEvents>)])]
public class PlainTransport<TPlainTransportAppData>
    : Transport<TPlainTransportAppData, PlainTransportEvents, PlainTransportObserver>, IPlainTransport
    where TPlainTransportAppData : new()
{
    /// <summary>
    /// Logger.
    /// </summary>
    private readonly ILogger logger = new Logger<PlainTransport<TPlainTransportAppData>>();

    /// <summary>
    /// Producer data.
    /// </summary>
    public override PlainTransportData Data { get; }

    public override PlainTransportObserver Observer => base.Observer.Sure<PlainTransportObserver>();

    /// <summary>
    /// <para>Events:</para>
    /// <para>@emits tuple - (tuple: TransportTuple)</para>
    /// <para>@emits rtcptuple - (rtcpTuple: TransportTuple)</para>
    /// <para>@emits sctpstatechange - (sctpState: SctpState)</para>
    /// <para>@emits trace - (trace: TransportTraceEventData)</para>
    /// <para>Observer events:</para>
    /// <para>@emits close</para>
    /// <para>@emits newproducer - (producer: Producer)</para>
    /// <para>@emits newconsumer - (consumer: Consumer)</para>
    /// <para>@emits newdataproducer - (dataProducer: DataProducer)</para>
    /// <para>@emits newdataconsumer - (dataConsumer: DataConsumer)</para>
    /// <para>@emits tuple - (tuple: TransportTuple)</para>
    /// <para>@emits rtcptuple - (rtcpTuple: TransportTuple)</para>
    /// <para>@emits sctpstatechange - (sctpState: SctpState)</para>
    /// <para>@emits trace - (trace: TransportTraceEventData)</para>
    /// </summary>
    public PlainTransport(PlainTransportConstructorOptions<TPlainTransportAppData> options)
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
        var data = response.Value.BodyAsPlainTransport_DumpResponse().UnPack();

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
        var data = response.Value.BodyAsPlainTransport_GetStatsResponse().UnPack();

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
            throw new Exception($"{nameof(parameters)} type is not FBS.PlainTransport.ConnectRequestT");
        }

        // Build Request
        var bufferBuilder = Channel.BufferPool.Get();

        var connectRequestOffset = ConnectRequest.Pack(bufferBuilder, connectRequestT);

        var response = await Channel.RequestAsync(bufferBuilder, Method.PLAINTRANSPORT_CONNECT,
            FBS.Request.Body.PlainTransport_ConnectRequest,
            connectRequestOffset.Value,
            Internal.TransportId);

        /* Decode Response. */
        var data = response.Value.BodyAsPlainTransport_ConnectResponse().UnPack();

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

                this.Emit(static x => x.tuple, Data.Tuple);

                // Emit observer event.
                Observer.Emit(static x => x.tuple, Data.Tuple);

                break;
            }
            case Event.PLAINTRANSPORT_RTCP_TUPLE:
            {
                var rtcpTupleNotification = notification.BodyAsPlainTransport_RtcpTupleNotification().UnPack();

                Data.RtcpTuple = rtcpTupleNotification.Tuple;

                this.Emit((PlainTransportEvents x) => x.rtcptuple, Data.RtcpTuple);

                // Emit observer event.
                Observer.Emit(static x => x.rtcptuple, Data.RtcpTuple);

                break;
            }
            case Event.TRANSPORT_SCTP_STATE_CHANGE:
            {
                var sctpStateChangeNotification = notification.BodyAsTransport_SctpStateChangeNotification().UnPack();

                Data.SctpState = sctpStateChangeNotification.SctpState;

                this.Emit(static x => x.sctpstatechange, Data.SctpState);

                // Emit observer event.
                Observer.Emit(static x => x.sctpstatechange, Data.SctpState);

                break;
            }
            case Event.TRANSPORT_TRACE:
            {
                var traceNotification = notification.BodyAsTransport_TraceNotification().UnPack();

                this.Emit(static x=>x.trace, traceNotification);

                // Emit observer event.
                Observer.Emit(static x => x.trace, traceNotification);

                break;
            }
            default:
            {
                logger.LogError("OnNotificationHandle() | PlainTransport:{TransportId} Ignoring unknown event:{@event}",
                    Id, @event);
                break;
            }
        }
    }

    #endregion Event Handlers
}