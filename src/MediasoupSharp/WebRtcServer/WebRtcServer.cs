using Microsoft.Extensions.Logging;

namespace MediasoupSharp.WebRtcServer;

public interface IWebRtcServer
{
    string Id { get; }

    internal void HandleWebRtcTransport<TWebRtcTransportAppData>(
        WebRtcTransport.WebRtcTransport<TWebRtcTransportAppData> webRtcTransport);
}

internal class WebRtcServer<TWebRtcServerAppData> : WebRtcServer
{
    public WebRtcServer(
        WebRtcServerInternal @internal, 
        Channel.Channel channel, 
        TWebRtcServerAppData? appData,
        ILoggerFactory? loggerFactory = null)
        : base(
            @internal,
            channel,
            appData,
            loggerFactory)
    {
        
    }

    public new TWebRtcServerAppData AppData
    {
        get => (TWebRtcServerAppData)base.AppData;
        set => base.AppData = value!;
    }
}

internal class WebRtcServer
    : EnhancedEventEmitter<object> , IWebRtcServer
{
    private readonly ILogger? logger;
    
    private readonly WebRtcServerInternal @internal;

    /// <summary>
    /// Channel instance.
    /// </summary>
    private readonly Channel.Channel channel;

    /// <summary>
    /// Closed flag.
    /// </summary>
    public bool Closed { get; private set; }

    /// <summary>
    /// Custom app data.
    /// </summary>
    public object AppData { get; set; }

    /// <summary>
    /// Transports map.
    /// </summary>
    private readonly Dictionary<string, WebRtcTransport.WebRtcTransport> webRtcTransports = new();

    /// <summary>
    /// Observer instance.
    /// </summary>
    public EnhancedEventEmitter<WebRtcServerObserverEvents> Observer { get; }

    public WebRtcServer(
        WebRtcServerInternal @internal,
        Channel.Channel channel,
        object? appData = null,
        ILoggerFactory? loggerFactory = null)
    : base(loggerFactory)
    {
        logger         = loggerFactory?.CreateLogger(GetType());
        this.@internal = @internal;
        this.channel   = channel;
        AppData        = appData ?? new();
        Observer       = new(loggerFactory);
    }

    public string Id => @internal.WebRtcServerId;

    public Dictionary<string, WebRtcTransport.WebRtcTransport> WebRtcTransportsForTesting => webRtcTransports;

    /// <summary>
    /// Close the WebRtcServer.
    /// </summary>
    public void Close()
    {
        if (Closed)
        {
            return;
        }

        logger?.LogDebug("CloseAsync() | WebRtcServer: {Id}", Id);

        Closed = true;

        // TODO : Naming
        var reqData = new { webRtcServerId = @internal.WebRtcServerId };

        channel.Request("worker.closeWebRtcServer", null, reqData)
            .ContinueWith(_ => { }, TaskContinuationOptions.OnlyOnFaulted);

        // Close every WebRtcTransport.
        foreach (var webRtcTransport in webRtcTransports.Values)
        {
            webRtcTransport.ListenServerClosed();

            // Emit observer event.
            _ = Observer.SafeEmit("webrtctransportunhandled", webRtcTransport);
        }

        webRtcTransports.Clear();

        _ = Emit("@close");

        // Emit observer event.
        _ = Observer.SafeEmit("close");
    }

    /// <summary>
    /// Worker was closed.
    /// </summary>
    public void WorkerClosed()
    {
        if (Closed)
        {
            return;
        }

        logger?.LogDebug("WorkerClosedAsync() | WebRtcServer: {Id}", Id);

        Closed = true;

        // NOTE: No need to close WebRtcTransports since they are closed by their
        // respective Router parents.
        webRtcTransports.Clear();

        _ = SafeEmit("workerclose");

        // Emit observer event.
        _ = Observer.SafeEmit("close");
    }


    /// <summary>
    /// Dump Router.
    /// </summary>
    public async Task<object> DumpAsync()
    {
        logger?.LogDebug("DumpAsync() | WebRtcServer: {Id}", Id);

        return (await channel.Request("webRtcServer.dump", @internal.WebRtcServerId))!;
    }

    public void HandleWebRtcTransport<TWebRtcTransportAppData>(
        WebRtcTransport.WebRtcTransport<TWebRtcTransportAppData> webRtcTransport)
    {
        webRtcTransports[webRtcTransport.Id] = webRtcTransport;

        // Emit observer event.
        _ = Observer.SafeEmit("webrtctransporthandled", webRtcTransport);

        webRtcTransport.On("@close", async args =>
        {
            webRtcTransports.Remove(webRtcTransport.Id);

            // Emit observer event.
            await Observer.SafeEmit("webrtctransportunhandled", webRtcTransport);
        });
    }
}