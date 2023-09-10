using MediasoupSharp.Channel;
using MediasoupSharp.Exceptions;

namespace MediasoupSharp.WebRtcServer;

public class WebRtcServer<TWebRtcServerAppData> : EnhancedEventEmitter<WebRtcServerEvents>
{
    /// <summary>
    /// Logger.
    /// </summary>
    private readonly ILogger<WebRtcServer> logger;

    #region Internal data.

    private readonly WebRtcServerInternal @internal;

    public string WebRtcServerId => @internal.WebRtcServerId;

    #endregion Internal data.

    /// <summary>
    /// Channel instance.
    /// </summary>
    private IChannel channel;

    /// <summary>
    /// Closed flag.
    /// </summary>
    private bool closed = false;

    /// <summary>
    /// Close locker.
    /// </summary>
    private readonly AsyncReaderWriterLock closeLock = new();

    /// <summary>
    /// Custom app data.
    /// </summary>
    public Dictionary<string, object> AppData { get; }

    /// <summary>
    /// Transports map.
    /// </summary>
    private readonly Dictionary<string, WebRtcTransport.WebRtcTransport> webRtcTransports = new();
    private readonly AsyncReaderWriterLock webRtcTransportsLock = new();

    /// <summary>
    /// Observer instance.
    /// </summary>
    public EventEmitter Observer { get; } = new EventEmitter();

    /// <summary>
    /// <para>Events:</para>
    /// <para>@emits workerclose</para>
    /// <para>@emits @close</para>
    /// <para>Observer events:</para>
    /// <para>@emits close</para>
    /// <para>@emits webrtctransporthandled - (webRtcTransport: WebRtcTransport)</para>
    /// <para>@emits webrtctransportunhandled - (webRtcTransport: WebRtcTransport)</para>
    /// </summary>
    /// <param name="loggerFactory"></param>
    /// <param name="internal"></param>
    /// <param name="channel"></param>
    /// <param name="appData"></param>
    public WebRtcServer(ILoggerFactory loggerFactory,
        WebRtcServerInternal @internal,
        IChannel channel,
        Dictionary<string, object>? appData)
    {
        logger = loggerFactory.CreateLogger<WebRtcServer>();

        this.@internal = @internal;
        this.channel = channel;
        AppData = appData ?? new Dictionary<string, object>();
    }

    /// <summary>
    /// Close the WebRtcServer.
    /// </summary>
    public async Task CloseAsync()
    {
        logger.LogDebug($"CloseAsync() | WebRtcServer: {WebRtcServerId}");

        using (await closeLock.WriteLockAsync())
        {
            if (closed)
            {
                return;
            }

            closed = true;

            var reqData = new { WebRtcServerId = @internal.WebRtcServerId };

            // Fire and forget
            channel.RequestAsync(MethodId.WORKER_CLOSE_WEBRTCSERVER, null, reqData).ContinueWithOnFaultedHandleLog(logger);

            await CloseInternalAsync();

            Emit("@close");

            // Emit observer event.
            Observer.Emit("close");
        };
    }

    /// <summary>
    /// Worker was closed.
    /// </summary>
    public async Task WorkerClosedAsync()
    {
        logger.LogDebug($"WorkerClosedAsync() | WebRtcServer: {WebRtcServerId}");

        using (await closeLock.WriteLockAsync())
        {
            if (closed)
            {
                return;
            }

            closed = true;

            await CloseInternalAsync();

            Emit("workerclose");

            // Emit observer event.
            Observer.Emit("close");
        }
    }

    private async Task CloseInternalAsync()
    {
        using (await webRtcTransportsLock.WriteLockAsync())
        {
            // Close every WebRtcTransport.
            foreach (var webRtcTransport in webRtcTransports.Values)
            {
                await webRtcTransport.ListenServerClosedAsync();

                // Emit observer event.
                Observer.Emit("webrtctransportunhandled", webRtcTransport);
            }

            webRtcTransports.Clear();
        }
    }

    /// <summary>
    /// Dump Router.
    /// </summary>
    public async Task<string> DumpAsync()
    {
        logger.LogDebug($"DumpAsync() | WebRtcServer: {WebRtcServerId}");

        using (await closeLock.ReadLockAsync())
        {
            if (closed)
            {
                throw new InvalidStateException("WebRtcServer closed");
            }

            return (await channel.RequestAsync(MethodId.WEBRTCSERVER_DUMP, @internal.WebRtcServerId))!;
        }
    }

    internal void HandleWebRtcTransport<TWebRtcTransportAppData>(WebRtcTransport.WebRtcTransport<TWebRtcTransportAppData> webRtcTransport)
    {
        webRtcTransports[webRtcTransport.Id] = webRtcTransport;

        // Emit observer event.
        Observer.Emit("webrtctransporthandled", webRtcTransport);

        webRtcTransport.On("@close", async args =>
        {
            using (await webRtcTransportsLock.WriteLockAsync())
            {
                webRtcTransports.Remove(webRtcTransport.Id);
            }

            // Emit observer event.
            Observer.Emit("webrtctransportunhandled", webRtcTransport);
        });
    }
}