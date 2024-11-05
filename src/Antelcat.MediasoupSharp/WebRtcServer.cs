using Antelcat.AutoGen.ComponentModel.Diagnostic;
using Antelcat.MediasoupSharp.Internals.Extensions;
using FBS.Request;
using FBS.Transport;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Threading;

namespace Antelcat.MediasoupSharp;

using WebRtcServerObserver = EnhancedEventEmitter<WebRtcServerObserverEvents>;

public record WebRtcServerOptions<TWebRtcServerAppData>
{
    /// <summary>
    /// Listen infos.
    /// </summary>
    public required ListenInfoT[] ListenInfos { get; set; }

    /// <summary>
    /// Custom application data.
    /// </summary>
    public TWebRtcServerAppData? AppData { get; set; }
}

public class WebRtcServerEvents
{
    public object? workerclose;

    public (string, Exception) listenererror;

    // Private events.
    public object? _close;
}

public class WebRtcServerObserverEvents
{
    public object?           close;
    public IWebRtcTransport? webrtctransporthandled;
    public IWebRtcTransport? webrtctransportunhandled;
}

public class WebRtcServerInternal
{
    public string WebRtcServerId { get; set; }
}

[AutoExtractInterface]
public class WebRtcServer<TWebRtcServerAppData> : EnhancedEventEmitter<WebRtcServerEvents>, IWebRtcServer
    where TWebRtcServerAppData : new()
{
    /// <summary>
    /// Logger.
    /// </summary>
    private readonly ILogger logger = new Logger<WebRtcServer<TWebRtcServerAppData>>();

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
    public TWebRtcServerAppData AppData { get; }

    /// <summary>
    /// Transports map.
    /// </summary>
    private readonly Dictionary<string, IWebRtcTransport> webRtcTransports = new();

    private readonly AsyncReaderWriterLock webRtcTransportsLock = new();

    /// <summary>
    /// Observer instance.
    /// </summary>
    public WebRtcServerObserver Observer { get; } = new();

    /// <summary>
    /// <para>Events:</para>
    /// <para>@emits workerclose</para>
    /// <para>@emits @close</para>
    /// <para>Observer events:</para>
    /// <para>@emits close</para>
    /// <para>@emits webrtctransporthandled - (webRtcTransport: WebRtcTransport)</para>
    /// <para>@emits webrtctransportunhandled - (webRtcTransport: WebRtcTransport)</para>
    /// </summary>
    public WebRtcServer(
        WebRtcServerInternal @internal,
        IChannel channel,
        TWebRtcServerAppData? appData)
    {
        this.@internal = @internal;
        this.channel   = channel;
        AppData        = appData ?? new();
    }

    /// <summary>
    /// Close the WebRtcServer.
    /// </summary>
    public async Task CloseAsync()
    {
        logger.LogDebug("CloseAsync() | WebRtcServerId:{WebRtcServerId}", Id);

        await using (await closeLock.WriteLockAsync())
        {
            if (closed)
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

            var closeWebRtcServerRequestOffset =
                FBS.Worker.CloseWebRtcServerRequest.Pack(bufferBuilder, closeWebRtcServerRequest);

            // Fire and forget
            channel.RequestAsync(bufferBuilder, Method.WORKER_WEBRTCSERVER_CLOSE,
                Body.Worker_CloseWebRtcServerRequest,
                closeWebRtcServerRequestOffset.Value
            ).ContinueWithOnFaultedHandleLog(logger);

            await CloseInternalAsync();

            this.Emit(static x => x._close);

            // Emit observer event.
            Observer.Emit(static x => x.close);
        }
    }

    /// <summary>
    /// Worker was closed.
    /// </summary>
    public async Task WorkerClosedAsync()
    {
        logger.LogDebug("WorkerClosedAsync() | WebRtcServerId:{WebRtcServerId}", Id);

        await using (await closeLock.WriteLockAsync())
        {
            if (closed)
            {
                return;
            }

            closed = true;

            await CloseInternalAsync();

            this.Emit(static x => x.workerclose);

            // Emit observer event.
            Observer.Emit(static x => x.close);
        }
    }

    private async Task CloseInternalAsync()
    {
        await using (await webRtcTransportsLock.WriteLockAsync())
        {
            // Close every WebRtcTransport.
            foreach (var webRtcTransport in webRtcTransports.Values)
            {
                await webRtcTransport.ListenServerClosedAsync();

                // Emit observer event.
                Observer.Emit(static x => x.webrtctransportunhandled, webRtcTransport);
            }

            webRtcTransports.Clear();
        }
    }

    /// <summary>
    /// Dump Router.
    /// </summary>
    public async Task<FBS.WebRtcServer.DumpResponseT> DumpAsync()
    {
        logger.LogDebug("DumpAsync() | WebRtcServerId:{WebRtcServerId}", Id);

        await using (await closeLock.ReadLockAsync())
        {
            if (closed)
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

    public async Task HandleWebRtcTransportAsync(IWebRtcTransport webRtcTransport)
    {
        await using (await webRtcTransportsLock.WriteLockAsync())
        {
            webRtcTransports[webRtcTransport.Id] = webRtcTransport;
        }

        // Emit observer event.
        Observer.Emit(static x => x.webrtctransporthandled, webRtcTransport);

        webRtcTransport.On<WebRtcTransportEvents, object?>(static x => x._close, async () =>
        {
            await using (await webRtcTransportsLock.WriteLockAsync())
            {
                webRtcTransports.Remove(webRtcTransport.Id);
            }

            // Emit observer event.
            Observer.Emit(static x => x.webrtctransportunhandled, webRtcTransport);
        });
    }
}