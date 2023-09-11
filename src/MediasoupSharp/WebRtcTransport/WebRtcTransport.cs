using MediasoupSharp.Channel;
using MediasoupSharp.Exceptions;
using MediasoupSharp.Transport;
using Microsoft.Extensions.Logging;

namespace MediasoupSharp.WebRtcTransport;

public interface IWebRtcTransport{}

internal class WebRtcTransport<TWebRtcTransportAppData> : WebRtcTransport
{
    public WebRtcTransport(WebRtcTransportConstructorOptions<object> options)
        : base(options)
    {
    }
}

internal class WebRtcTransport
    : Transport<object, WebRtcTransportEvents, WebRtcTransportObserverEvents>
{
    readonly WebRtcTransportData data;
    
    public WebRtcTransport(
        WebRtcTransportConstructorOptions<object> options) 
        : base(options)
    {
        data = options.Data.DeepClone();
        
        HandleWorkerNotifications();
    }

    /// <summary>
    /// ICE role.
    /// </summary>
    public string IceRole => data.IceRole;

    /// <summary>
    /// ICE parameters.
    /// </summary>
    public IceParameters IceParameters => data.IceParameters;

    /// <summary>
    /// ICE candidates.
    /// </summary>
    public List<IceCandidate> IceCandidates => data.IceCandidates;

    /// <summary>
    /// ICE state.
    /// </summary>
    public IceState IceState => data.IceState;

    /// <summary>
    /// ICE selected tuple.
    /// </summary>
    public TransportTuple? IceSelectedTuple => data.IceSelectedTuple;

    /// <summary>
    /// DTLS parameters.
    /// </summary>
    public DtlsParameters DtlsParameters => data.DtlsParameters;

    /// <summary>
    /// DTLS state.
    /// </summary>
    /// <returns></returns>
    public DtlsState DtlsState => data.DtlsState;

    /// <summary>
    /// Remote certificate in PEM format.
    /// </summary>
    public string? DtlsRemoteCert => data.DtlsRemoteCert;

    /// <summary>
    /// SCTP parameters.
    /// </summary>
    public SctpParameters.SctpParameters? SctpParameters => data.SctpParameters;

    /// <summary>
    /// SCTP state.
    /// </summary>
    public SctpState? SctpState => data.SctpState;
    
    
    /// <summary>
    /// Close the WebRtcTransport.
    /// </summary>
    public override void Close()
    {
        if (Closed)
        {
            return; 
        }
        
        data.IceState = IceState.closed;
        data.IceSelectedTuple = null;
        data.DtlsState = DtlsState.closed;

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

        data.IceState = IceState.closed;
        data.IceSelectedTuple = null;
        data.DtlsState = DtlsState.closed;

        if (data.SctpState.HasValue)
        {
            data.SctpState = Transport.SctpState.closed;
        }

        base.RouterClosed();
    }

    /// <summary>
    /// Called when closing the associated listenServer (WebRtcServer).
    /// </summary>
    internal override void ListenServerClosed()
    {
        if (Closed)
        {
            return;
        }

        data.IceState = IceState.closed;
        data.IceSelectedTuple = null;
        data.DtlsState = DtlsState.closed;

        if (data.SctpState.HasValue)
        {
            data.SctpState = Transport.SctpState.closed;
        }

        base.ListenServerClosed();
    }


    public new async Task<List<WebRtcTransportStat>> GetStatsAsync()
    {
        Logger?.LogDebug("getStats()");

        return (await Channel.Request("transport.getStats", Internal.TransportId)
            as List<WebRtcTransportStat>)!;
    }

    /// <summary>
    /// Provide the WebRtcTransport remote parameters.
    /// </summary>
    public override async Task ConnectAsync(dynamic arg)
    {
        var dtlsParameters = arg.dtlsParameters as DtlsParameters;
        
        Logger?.LogDebug("ConnectAsync() | WebRtcTransport:{Id}", Id);
        
        var reqData = new { dtlsParameters };

        var data =
            (await Channel.Request("transport.connect", Internal.TransportId, reqData) as dynamic)!;
        
        // Update data.
        // TODO : Naming
        this.data.DtlsParameters.Role = data.DtlsLocalRole;
    }



    /// <summary>
    /// Restart ICE.
    /// </summary>
    public async Task<IceParameters> RestartIceAsync()
    {
        Logger?.LogDebug("RestartIceAsync() | WebRtcTransport:{Id}", Id);

        // TODO : Naming
        var data = (await Channel.Request("transport.restartIce", Internal.TransportId)
            as dynamic)!;

        var iceParameters = data.IceParameters;
        
        // Update data.
        data.IceParameters = iceParameters;

        return iceParameters;
    }

    private void HandleWorkerNotifications()
    {
        Channel.On(Internal.TransportId, async args =>
        {
            var @event = args![0] as string;
            var data = args[1] as dynamic;
            switch (@event)
            {
                case "icestatechange":
                {
                    var iceState = (IceState)data.iceState;

                    this.data.IceState = iceState;

                    await SafeEmit("icestatechange", iceState);

                    // Emit observer event.
                    await Observer.SafeEmit("icestatechange", iceState);

                    break;
                }

                case "iceselectedtuplechange":
                {
                    var iceSelectedTuple = (data.iceSelectedTuple as TransportTuple)!;

                    this.data.IceSelectedTuple = iceSelectedTuple;

                    await SafeEmit("iceselectedtuplechange", iceSelectedTuple);

                    // Emit observer event.
                    await Observer.SafeEmit("iceselectedtuplechange", iceSelectedTuple);

                    break;
                }

                case "dtlsstatechange":
                {
                    var dtlsState = (DtlsState)data.dtlsState;
                    var dtlsRemoteCert = (data.dtlsRemoteCert as string)!;

                    this.data.DtlsState = dtlsState;

                    if (dtlsState == DtlsState.connected)
                    {
                        this.data.DtlsRemoteCert = dtlsRemoteCert;
                    }

                    await SafeEmit("dtlsstatechange", dtlsState);

                    // Emit observer event.
                    await Observer.SafeEmit("dtlsstatechange", dtlsState);

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
                    Logger?.LogError("OnChannelMessage() | WebRtcTransport:{Id} Ignoring unknown event{Event}", Id,
                        @event);

                    break;
                }
            }
        });
    }
}