using MediasoupSharp.Transport;
using Microsoft.Extensions.Logging;

namespace MediasoupSharp.PlainTransport;

internal class PlainTransport<TPlainTransportAppData>
    : Transport<TPlainTransportAppData, PlainTransportEvents, PlainTransportObserverEvents>
{
    private readonly ILogger? logger;
    /// <summary>
    /// Producer data.
    /// </summary>
    private readonly PlainTransportData data;

    public PlainTransport(
        PlainTransportConstructorOptions<TPlainTransportAppData> options,
        ILoggerFactory? loggerFactory = null
    ) : base(options,loggerFactory)
    {
        logger = loggerFactory?.CreateLogger(GetType());
        
        data = options.Data with { };

        HandleWorkerNotifications();
    }

    public TransportTuple Tuple => data.Tuple;

    public TransportTuple? RtcpTuple => data.RtcpTuple;

    public SctpParameters.SctpParameters? SctpParameters => data.SctpParameters;

    public SctpState? SctpState => data.SctpState;

    public SrtpParameters.SrtpParameters? SrtpParameters => data.SrtpParameters;

    /// <summary>
    /// Close the PlainTransport.
    /// </summary>
    public override void Close()
    {
        if (Closed)
        {
            return;
        }

        if (data.SctpState.HasValue)
        {
            data.SctpState = Transport.SctpState.closed;
        }

        base.Close();
    }

    /// <summary>
    /// Router was closed.
    /// </summary>
    public override void RouterClosed()
    {
        if (Closed)
        {
            return;
        }

        if (data.SctpState.HasValue)
        {
            data.SctpState = Transport.SctpState.closed;
        }

        base.RouterClosed();
    }

    public new async Task<List<PlainTransportStat>> GetStatsAsync()
    {
        logger?.LogDebug("getStats()");

        return await Channel.Request("transport.getStats", Internal.TransportId) as List<PlainTransportStat>;
    }

    /// <summary>
    /// Provide the PipeTransport remote parameters.
    /// </summary>
    public override async Task ConnectAsync(object parameters)
    {
        logger?.LogDebug("ConnectAsync()");

        // TODO : Naming
        var data =
            await Channel.Request("transport.connect", Internal.TransportId, parameters) as dynamic;

        // Update data.
        if (data.tuple)
        {
            this.data.Tuple = data.tuple;
        }

        if (data.rtcpTuple)
        {
            this.data.RtcpTuple = data.rtcpTuple;
        }

        this.data.SrtpParameters = data.srtpParameters;
    }


    private void HandleWorkerNotifications()
    {
        Channel.On(Internal.TransportId, async (args) =>
        {
            var @event = args![0] as string;
            var data   = args[1] as dynamic;
            switch (@event)
            {
                case "tuple":
                {
                    var tuple = (data.tuple as TransportTuple)!;

                    this.data.Tuple = tuple;

                    await SafeEmit("tuple", tuple);

                    // Emit observer event.
                    await Observer.SafeEmit("tuple", tuple);

                    break;
                }

                case "rtcptuple":
                {
                    var rtcpTuple = (data.rtcpTuple as TransportTuple)!;

                    this.data.RtcpTuple = rtcpTuple;

                    await SafeEmit("rtcptuple", rtcpTuple);

                    // Emit observer event.
                    await Observer.SafeEmit("rtcptuple", rtcpTuple);

                    break;
                }

                case "sctpstatechange":
                {
                    var sctpState = (SctpState)data.sctpState;

                    this.data.SctpState = sctpState;

                    await SafeEmit("sctpstatechange", sctpState);

                    // Emit observer event.
                    await Observer.SafeEmit("sctpstatechange", sctpState);

                    break;
                }

                case "trace":
                {
                    var trace = (data as TransportTraceEventData)!;

                    await SafeEmit("trace", trace);

                    // Emit observer event.
                    await Observer.SafeEmit("trace", trace);

                    break;
                }

                default:
                {
                    logger?.LogError("ignoring unknown event {E}", @event);
                    break;
                }
            }
        });
    }
}