using FBS.Request;
using FBS.Transport;
using FBS.Worker;
using MediasoupSharp.Channel;
using MediasoupSharp.Constants;
using MediasoupSharp.Exceptions;
using MediasoupSharp.Internals.Extensions;
using MediasoupSharp.Router;
using MediasoupSharp.Settings;
using MediasoupSharp.WebRtcServer;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Threading;

namespace MediasoupSharp.Worker;

public abstract class WorkerBase : EnhancedEvent.EventEmitter, IDisposable, IWorker
{
    #region Protected Fields

    /// <summary>
    /// Logger factory for create logger.
    /// </summary>
    protected readonly ILoggerFactory LoggerFactory;

    /// <summary>
    /// Logger.
    /// </summary>
    protected readonly ILogger<Worker> Logger;

    /// <summary>
    /// Channel instance.
    /// </summary>
    protected IChannel Channel;

    /// <summary>
    /// Router set.
    /// </summary>
    protected readonly List<Router.Router> Routers = [];

    /// <summary>
    /// _routers locker.
    /// </summary>
    protected readonly object RoutersLock = new();

    /// <summary>
    /// WebRtcServers set.
    /// </summary>
    protected readonly List<WebRtcServer.WebRtcServer> WebRtcServers = [];

    /// <summary>
    /// _webRtcServer locker.
    /// </summary>
    protected readonly object WebRtcServersLock = new();

    /// <summary>
    /// Closed flag.
    /// </summary>
    protected bool Closed;

    /// <summary>
    /// Close locker.
    /// </summary>
    protected readonly AsyncReaderWriterLock CloseLock = new();

    #endregion Protected Fields

    /// <summary>
    /// Custom app data.
    /// </summary>
    public Dictionary<string, object> AppData { get; }

    /// <summary>
    /// Observer instance.
    /// </summary>
    public EnhancedEvent.EventEmitter Observer { get; } = new();

    /// <summary>
    /// <para>Events:</para>
    /// <para>@emits died - (error: Error)</para>
    /// <para>@emits @success</para>
    /// <para>@emits @failure - (error: Error)</para>
    /// <para>Observer events:</para>
    /// <para>@emits close</para>
    /// <para>@emits newwebrtcserver - (webRtcServer: WebRtcServer)</para>
    /// <para>@emits newrouter - (router: Router)</para>
    /// </summary>
    protected WorkerBase(ILoggerFactory loggerFactory, MediasoupOptions mediasoupOptions)
    {
        LoggerFactory = loggerFactory;
        Logger         = loggerFactory.CreateLogger<Worker>();

        var workerSettings = mediasoupOptions.MediasoupSettings.WorkerSettings;

        AppData = workerSettings.AppData ?? new Dictionary<string, object>();
    }

    public abstract Task CloseAsync();

    #region Request

    /// <summary>
    /// Dump Worker.
    /// </summary>
    public async Task<DumpResponseT> DumpAsync()
    {
        Logger.LogDebug("DumpAsync()");

        await using(await CloseLock.ReadLockAsync())
        {
            if(Closed)
            {
                throw new InvalidStateException("Worker closed");
            }

            // Build Request
            var bufferBuilder = Channel.BufferPool.Get();

            var response = await Channel.RequestAsync(bufferBuilder, Method.WORKER_DUMP);
            var data     = response.Value.BodyAsWorker_DumpResponse().UnPack();
            return data;
        }
    }

    /// <summary>
    /// Get mediasoup-worker process resource usage.
    /// </summary>
    public async Task<ResourceUsageResponseT> GetResourceUsageAsync()
    {
        Logger.LogDebug("GetResourceUsageAsync()");

        await using(await CloseLock.ReadLockAsync())
        {
            if(Closed)
            {
                throw new InvalidStateException("Worker closed");
            }

            // Build Request
            var bufferBuilder = Channel.BufferPool.Get();

            var response = await Channel.RequestAsync(bufferBuilder, Method.WORKER_GET_RESOURCE_USAGE);
            var data     = response.Value.BodyAsWorker_ResourceUsageResponse().UnPack();

            return data;
        }
    }

    /// <summary>
    /// Updates the worker settings in runtime. Just a subset of the worker settings can be updated.
    /// </summary>
    public async Task UpdateSettingsAsync(WorkerUpdateableSettings workerUpdateableSettings)
    {
        Logger.LogDebug("UpdateSettingsAsync()");

        await using(await CloseLock.ReadLockAsync())
        {
            if(Closed)
            {
                throw new InvalidStateException("Worker closed");
            }

            var logLevel       = workerUpdateableSettings.LogLevel ?? WorkerLogLevel.None;
            var logLevelString = logLevel.GetEnumText();
            var logTags        = workerUpdateableSettings.LogTags ?? [];
            var logTagStrings  = logTags.Select(m => m.GetEnumText()).ToList();

            // Build Request
            var bufferBuilder = Channel.BufferPool.Get();

            var updateSettingsRequestT = new UpdateSettingsRequestT
            {
                LogLevel = logLevelString,
                LogTags  = logTagStrings
            };

            var requestOffset = UpdateSettingsRequest.Pack(bufferBuilder, updateSettingsRequestT);

            // Fire and forget
            Channel.RequestAsync(bufferBuilder, Method.WORKER_UPDATE_SETTINGS,
                Body.Worker_UpdateSettingsRequest,
                requestOffset.Value
            ).ContinueWithOnFaultedHandleLog(Logger);
        }
    }

    /// <summary>
    /// Create a WebRtcServer.
    /// </summary>
    /// <returns>WebRtcServer</returns>
    public async Task<WebRtcServer.WebRtcServer> CreateWebRtcServerAsync(WebRtcServerOptions webRtcServerOptions)
    {
        Logger.LogDebug("CreateWebRtcServerAsync()");

        await using(await CloseLock.ReadLockAsync())
        {
            if(Closed)
            {
                throw new InvalidStateException("Workder closed");
            }

            // Build the request.
            var fbsListenInfos = webRtcServerOptions.ListenInfos.Select(m => new ListenInfoT
            {
                Protocol         = m.Protocol,
                Ip               = m.Ip,
                AnnouncedAddress = m.AnnouncedAddress,
                Port             = m.Port,
                PortRange        = m.PortRange,
                Flags            = m.Flags,
                SendBufferSize   = m.SendBufferSize,
                RecvBufferSize   = m.RecvBufferSize
            }).ToList();

            var webRtcServerId = Guid.NewGuid().ToString();

            // Build Request
            var bufferBuilder = Channel.BufferPool.Get();

            var createWebRtcServerRequestT = new CreateWebRtcServerRequestT
            {
                WebRtcServerId = webRtcServerId,
                ListenInfos    = fbsListenInfos
            };

            var createWebRtcServerRequestOffset = CreateWebRtcServerRequest.Pack(bufferBuilder, createWebRtcServerRequestT);

            await Channel.RequestAsync(bufferBuilder, Method.WORKER_CREATE_WEBRTCSERVER,
                Body.Worker_CreateWebRtcServerRequest,
                createWebRtcServerRequestOffset.Value
            );

            var webRtcServer = new WebRtcServer.WebRtcServer(
                LoggerFactory,
                new WebRtcServerInternal { WebRtcServerId = webRtcServerId },
                Channel,
                webRtcServerOptions.AppData
            );

            lock(WebRtcServersLock)
            {
                WebRtcServers.Add(webRtcServer);
            }

            webRtcServer.On(
                "@close",
                (_, _) =>
                {
                    lock(WebRtcServersLock)
                    {
                        WebRtcServers.Remove(webRtcServer);
                    }

                    return Task.CompletedTask;
                }
            );

            // Emit observer event.
            Observer.Emit("newwebrtcserver", webRtcServer);

            return webRtcServer;
        }
    }

    /// <summary>
    /// Create a Router.
    /// </summary>
    /// <returns>Router</returns>
    public async Task<Router.Router> CreateRouterAsync(RouterOptions routerOptions)
    {
        Logger.LogDebug("CreateRouterAsync()");

        await using(await CloseLock.ReadLockAsync())
        {
            if(Closed)
            {
                throw new InvalidStateException("Workder closed");
            }

            // This may throw.
            var rtpCapabilities = ORTC.Ortc.GenerateRouterRtpCapabilities(routerOptions.MediaCodecs);

            var routerId = Guid.NewGuid().ToString();

            // Build Request
            var bufferBuilder = Channel.BufferPool.Get();

            var createRouterRequestT = new CreateRouterRequestT
            {
                RouterId = routerId
            };

            var createRouterRequestOffset = CreateRouterRequest.Pack(bufferBuilder, createRouterRequestT);

            await Channel.RequestAsync(bufferBuilder, Method.WORKER_CREATE_ROUTER,
                Body.Worker_CreateRouterRequest,
                createRouterRequestOffset.Value);

            var router = new Router.Router(
                LoggerFactory,
                new RouterInternal(routerId),
                new RouterData { RtpCapabilities = rtpCapabilities },
                Channel,
                routerOptions.AppData
            );

            lock(RoutersLock)
            {
                Routers.Add(router);
            }

            router.On(
                "@close",
                (_, _) =>
                {
                    lock(RoutersLock)
                    {
                        Routers.Remove(router);
                    }

                    return Task.CompletedTask;
                }
            );

            // Emit observer event.
            Observer.Emit("newrouter", router);

            return router;
        }
    }

    #endregion Request

    #region IDisposable Support

    private bool disposedValue; // 要检测冗余调用

    protected virtual void DestroyManaged() { }

    protected virtual void DestroyUnmanaged() { }

    protected virtual void Dispose(bool disposing)
    {
        if (disposedValue) return;
        if(disposing)
        {
            // TODO: 释放托管状态(托管对象)。
            DestroyManaged();
        }

        // TODO: 释放未托管的资源(未托管的对象)并在以下内容中替代终结器。
        // TODO: 将大型字段设置为 null。
        DestroyUnmanaged();

        disposedValue = true;
    }

    // TODO: 仅当以上 Dispose(bool disposing) 拥有用于释放未托管资源的代码时才替代终结器。
    ~WorkerBase()
    {
        // 请勿更改此代码。将清理代码放入以上 Dispose(bool disposing) 中。
        Dispose(false);
    }

    // 添加此代码以正确实现可处置模式。
    public void Dispose()
    {
        // 请勿更改此代码。将清理代码放入以上 Dispose(bool disposing) 中。
        Dispose(true);
        // TODO: 如果在以上内容中替代了终结器，则取消注释以下行。
        GC.SuppressFinalize(this);
    }

    #endregion IDisposable Support
}