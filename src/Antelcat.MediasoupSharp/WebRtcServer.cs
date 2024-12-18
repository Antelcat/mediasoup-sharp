﻿using Antelcat.AutoGen.ComponentModel.Diagnostic;
using Antelcat.MediasoupSharp.FBS.Request;
using Antelcat.MediasoupSharp.Internals.Collections;
using Antelcat.MediasoupSharp.Internals.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Threading;

namespace Antelcat.MediasoupSharp;

public class WebRtcServerInternal
{
    public required string WebRtcServerId { get; set; }
}

[AutoExtractInterface(NamingTemplate = nameof(IWebRtcServer))]
public class WebRtcServerImpl<TWebRtcServerAppData> 
    : EnhancedEventEmitter<WebRtcServerEvents>, IWebRtcServer<TWebRtcServerAppData>
    where TWebRtcServerAppData : new()
{
    /// <summary>
    /// Logger.
    /// </summary>
    private readonly ILogger logger = new Logger<IWebRtcTransport>();

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
    private readonly AsyncReaderWriterLock closeLock = new(null);

    /// <summary>
    /// Custom app data.
    /// </summary>
    public TWebRtcServerAppData AppData { get; set; }

    /// <summary>
    /// Transports map.
    /// </summary>
    private readonly AsyncReadWriteDictionary<string, IWebRtcTransport> webRtcTransports = new();

    /// <summary>
    /// Observer instance.
    /// </summary>
    public WebRtcServerObserver Observer { get; } = new();

    /// <summary>
    /// <para>Events : <see cref="WebRtcServerEvents"/></para>
    /// <para>Observer events : <see cref="WebRtcServerObserverEvents"/></para>
    /// </summary>
    public WebRtcServerImpl(
        WebRtcServerInternal @internal,
        IChannel channel,
        TWebRtcServerAppData? appData)
    {
        this.@internal = @internal;
        this.channel   = channel;
        AppData        = appData ?? new();
        
        HandleListenerError();
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

            // Fire and forget
            channel.RequestAsync(bufferBuilder => 
                    Antelcat.MediasoupSharp.FBS.Worker.CloseWebRtcServerRequest.Pack(
                    bufferBuilder, new Antelcat.MediasoupSharp.FBS.Worker.CloseWebRtcServerRequestT
                    {
                        WebRtcServerId = @internal.WebRtcServerId
                    }).Value,
                Method.WORKER_WEBRTCSERVER_CLOSE,
                Body.Worker_CloseWebRtcServerRequest
            ).ContinueWithOnFaultedHandleLog(logger);

            await CloseInternalAsync();

            this.Emit(static x => x.close);

            // Emit observer event.
            Observer.SafeEmit(static x => x.Close);
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

            this.SafeEmit(static x => x.WorkerClose);

            // Emit observer event.
            Observer.SafeEmit(static x => x.Close);
        }
    }

    private async Task CloseInternalAsync()
    {
        await webRtcTransports.WriteAsync(async x=>
        {
            // Close every WebRtcTransport.
            foreach (var webRtcTransport in x.Values)
            {
                await webRtcTransport.ListenServerClosedAsync();

                // Emit observer event.
                Observer.SafeEmit(static x => x.WebrtcTransportUnhandled, webRtcTransport);
            }

            x.Clear();
        });
    }

    /// <summary>
    /// Dump Router.
    /// </summary>
    public async Task<Antelcat.MediasoupSharp.FBS.WebRtcServer.DumpResponseT> DumpAsync()
    {
        logger.LogDebug("DumpAsync() | WebRtcServerId:{WebRtcServerId}", Id);

        await using (await closeLock.ReadLockAsync())
        {
            if (closed)
            {
                throw new InvalidStateException("WebRtcServer closed");
            }

            var response = await channel.RequestAsync(static _ => null,
                Method.WEBRTCSERVER_DUMP,
                null,
                @internal.WebRtcServerId);

            /* Decode Response. */
            var data = response.NotNull().BodyAsWebRtcServer_DumpResponse().UnPack();
            return data;
        }
    }

    public async Task HandleWebRtcTransportAsync(IWebRtcTransport webRtcTransport)
    {
        await webRtcTransports.WriteAsync(x=>
        {
            x[webRtcTransport.Id] = webRtcTransport;
        });

        // Emit observer event.
        Observer.SafeEmit(static x => x.WebrtcTransportHandled, webRtcTransport);

        webRtcTransport.On(static x => x.close, async () =>
        {
            await webRtcTransports.WriteAsync(x => x.Remove(webRtcTransport.Id));

            // Emit observer event.
            Observer.SafeEmit(static x => x.WebrtcTransportUnhandled, webRtcTransport);
        });
    }
    
    private void HandleListenerError() =>
        this.On(static x => x.ListenerError, tuple =>
        {
            logger.LogError(tuple.error,
                "event listener threw an error [eventName:{EventName}]:",
                tuple.eventName);
        });
}