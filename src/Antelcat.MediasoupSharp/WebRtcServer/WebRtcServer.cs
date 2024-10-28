using Antelcat.MediasoupSharp.Channel;
using Antelcat.MediasoupSharp.Exceptions;
using FBS.Request;
using FBS.WebRtcServer;
using Antelcat.MediasoupSharp.Internals.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Threading;

namespace Antelcat.MediasoupSharp.WebRtcServer;

public class WebRtcServer : EnhancedEvent.EnhancedEventEmitter
{
    /// <summary>
    /// Logger.
    /// </summary>
    private readonly ILogger<WebRtcServer> logger;

    #region Internal data.

    private readonly WebRtcServerInternal @internal;

    public string Id => @internal.WebRtcServerId;

    #endregion Internal data.

    /// <summary>
    /// Channel instance.
    /// </summary>
    private readonly IChannel channel;

    /// <summary>
    /// Closed flag.
    /// </summary>
    private bool closed;

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
    public EnhancedEvent.EnhancedEventEmitter Observer { get; } = new();

    /// <summary>
    /// <para>Events:</para>
    /// <para>@emits workerclose</para>
    /// <para>@emits @close</para>
    /// <para>Observer events:</para>
    /// <para>@emits close</para>
    /// <para>@emits webrtctransporthandled - (webRtcTransport: WebRtcTransport)</para>
    /// <para>@emits webrtctransportunhandled - (webRtcTransport: WebRtcTransport)</para>
    /// </summary>
    public WebRtcServer(ILoggerFactory loggerFactory,
                        WebRtcServerInternal @internal,
                        IChannel channel,
                        Dictionary<string, object>? appData)
    {
        logger = loggerFactory.CreateLogger<WebRtcServer>();

        this.@internal = @internal;
        this.channel   = channel;
        AppData        = appData ?? new Dictionary<string, object>();
    }

    /// <summary>
    /// Close the WebRtcServer.
    /// </summary>
    public async Task CloseAsync()
    {
        logger.LogDebug("CloseAsync() | WebRtcServerId:{WebRtcServerId}", Id);

        await using(await closeLock.WriteLockAsync())
        {
            if(closed)
            {
                return;
            }

            closed = true;

            // Build Request
            var bufferBuilder = channel.BufferPool.Get();

            var closeWebRtcServerRequest = new FBS.Worker.CloseWebRtcServerRequestT
            {
                WebRtcServerId = @internal.WebRtcServerId,
            };

            var closeWebRtcServerRequestOffset = FBS.Worker.CloseWebRtcServerRequest.Pack(bufferBuilder, closeWebRtcServerRequest);

            // Fire and forget
            channel.RequestAsync(bufferBuilder, Method.WORKER_WEBRTCSERVER_CLOSE,
                Body.Worker_CloseWebRtcServerRequest,
                closeWebRtcServerRequestOffset.Value
            ).ContinueWithOnFaultedHandleLog(logger);

            await CloseInternalAsync();

            Emit("@close");

            // Emit observer event.
            Observer.Emit("close");
        }
    }

    /// <summary>
    /// Worker was closed.
    /// </summary>
    public async Task WorkerClosedAsync()
    {
        logger.LogDebug("WorkerClosedAsync() | WebRtcServerId:{WebRtcServerId}", Id);

        await using(await closeLock.WriteLockAsync())
        {
            if(closed)
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
        await using(await webRtcTransportsLock.WriteLockAsync())
        {
            // Close every WebRtcTransport.
            foreach(var webRtcTransport in webRtcTransports.Values)
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
    public async Task<DumpResponseT> DumpAsync()
    {
        logger.LogDebug("DumpAsync() | WebRtcServerId:{WebRtcServerId}", Id);

        await using(await closeLock.ReadLockAsync())
        {
            if(closed)
            {
                throw new InvalidStateException("WebRtcServer closed");
            }

            // Build Request
            var bufferBuilder = channel.BufferPool.Get();

            var response = await channel.RequestAsync(bufferBuilder, Method.WEBRTCSERVER_DUMP,
                null,
                null,
                @internal.WebRtcServerId);

            /* Decode Response. */
            var data = response.Value.BodyAsWebRtcServer_DumpResponse().UnPack();
            return data;
        }
    }

    public async Task HandleWebRtcTransportAsync(WebRtcTransport.WebRtcTransport webRtcTransport)
    {
        await using(await webRtcTransportsLock.WriteLockAsync())
        {
            webRtcTransports[webRtcTransport.Id] = webRtcTransport;
        }

        // Emit observer event.
        Observer.Emit("webrtctransporthandled", webRtcTransport);

        webRtcTransport.On("@close", async _ =>
        {
            await using(await webRtcTransportsLock.WriteLockAsync())
            {
                webRtcTransports.Remove(webRtcTransport.Id);
            }

            // Emit observer event.
            Observer.Emit("webrtctransportunhandled", webRtcTransport);
        });
    }
}