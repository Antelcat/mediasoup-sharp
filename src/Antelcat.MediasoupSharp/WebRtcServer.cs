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

public abstract class WebRtcServerEvents
{
    public object? WorkerClose;

    public (string, Exception) ListenerError;

    // Private events.
    public object? close;
}

public abstract class WebRtcServerObserverEvents
{
    public          object?          Close;
    public required IWebRtcTransport WebrtcTransportHandled;
    public required IWebRtcTransport WebrtcTransportUnhandled;
}

public class WebRtcServerInternal
{
    public required string WebRtcServerId { get; set; }
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
    /// <para>@emits <see cref="WebRtcServerEvents.WorkerClose"/></para>
    /// <para>@emits <see cref="WebRtcServerEvents.close"/></para>
    /// <para>Observer events:</para>
    /// <para>@emits <see cref="WebRtcServerObserverEvents.Close"/></para>
    /// <para>@emits <see cref="WebRtcServerObserverEvents.WebrtcTransportHandled"/> - (webRtcTransport: WebRtcTransport)</para>
    /// <para>@emits <see cref="WebRtcServerObserverEvents.WebrtcTransportUnhandled"/> - (webRtcTransport: WebRtcTransport)</para>
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
                WebRtcServerId = @internal.WebRtcServerId
            };

            var closeWebRtcServerRequestOffset =
                FBS.Worker.CloseWebRtcServerRequest.Pack(bufferBuilder, closeWebRtcServerRequest);

            // Fire and forget
            channel.RequestAsync(bufferBuilder, Method.WORKER_WEBRTCSERVER_CLOSE,
                Body.Worker_CloseWebRtcServerRequest,
                closeWebRtcServerRequestOffset.Value
            ).ContinueWithOnFaultedHandleLog(logger);

            await CloseInternalAsync();

            this.Emit(static x => x.close);

            // Emit observer event.
            Observer.Emit(static x => x.Close);
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

            this.Emit(static x => x.WorkerClose);

            // Emit observer event.
            Observer.Emit(static x => x.Close);
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
                Observer.Emit(static x => x.WebrtcTransportUnhandled, webRtcTransport);
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
            var data = response.NotNull().BodyAsWebRtcServer_DumpResponse().UnPack();
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
        Observer.Emit(static x => x.WebrtcTransportHandled, webRtcTransport);

        ((IEnhancedEventEmitter<WebRtcTransportEvents>)webRtcTransport).On(static x => x.close, async () =>
        {
            await using (await webRtcTransportsLock.WriteLockAsync())
            {
                webRtcTransports.Remove(webRtcTransport.Id);
            }

            // Emit observer event.
            Observer.Emit(static x => x.WebrtcTransportUnhandled, webRtcTransport);
        });
    }
}