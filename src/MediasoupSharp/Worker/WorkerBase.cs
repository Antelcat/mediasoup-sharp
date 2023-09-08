using System.Runtime.Serialization;
using MediasoupSharp.Channel;
using MediasoupSharp.Exceptions;
using MediasoupSharp.PayloadChannel;
using MediasoupSharp.Router;
using MediasoupSharp.Settings;
using MediasoupSharp.WebRtcServer;

namespace MediasoupSharp.Worker;

public abstract class WorkerBase : EventEmitter, IDisposable, IWorker
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
    /// PayloadChannel instance.
    /// </summary>
    protected IPayloadChannel PayloadChannel;

    /// <summary>
    /// Router set.
    /// </summary>
    protected readonly List<Router.Router> Routers = new();

    /// <summary>
    /// _routers locker.
    /// </summary>
    protected readonly object RoutersLock = new();

    /// <summary>
    /// WebRtcServers set.
    /// </summary>
    protected readonly List<WebRtcServer.WebRtcServer> WebRtcServers = new();

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
    public EventEmitter Observer { get; } = new EventEmitter();

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
    /// <param name="loggerFactory"></param>
    /// <param name="mediasoupOptions"></param>
    public WorkerBase(ILoggerFactory loggerFactory, MediasoupOptions mediasoupOptions)
    {
        LoggerFactory = loggerFactory;
        Logger = loggerFactory.CreateLogger<Worker>();

        var workerSettings = mediasoupOptions.MediasoupSettings.WorkerSettings;

        AppData = workerSettings.AppData ?? new Dictionary<string, object>();
    }

    public abstract Task CloseAsync();

    #region Request

    /// <summary>
    /// Dump Worker.
    /// </summary>
    public async Task<string> DumpAsync()
    {
        Logger.LogDebug("DumpAsync()");

        using (await CloseLock.ReadLockAsync())
        {
            if (Closed)
            {
                throw new InvalidStateException("Worker closed");
            }

            return (await Channel.RequestAsync(MethodId.WORKER_DUMP))!;
        }
    }

    /// <summary>
    /// Get mediasoup-worker process resource usage.
    /// </summary>
    public async Task<string> GetResourceUsageAsync()
    {
        Logger.LogDebug("GetResourceUsageAsync()");

        using (await CloseLock.ReadLockAsync())
        {
            if (Closed)
            {
                throw new InvalidStateException("Worker closed");
            }

            return (await Channel.RequestAsync(MethodId.WORKER_GET_RESOURCE_USAGE))!;
        }
    }

    /// <summary>
    /// Updates the worker settings in runtime. Just a subset of the worker settings can be updated.
    /// </summary>
    public async Task UpdateSettingsAsync(WorkerUpdateableSettings workerUpdateableSettings)
    {
        Logger.LogDebug("UpdateSettingsAsync()");

        using (await CloseLock.ReadLockAsync())
        {
            if (Closed)
            {
                throw new InvalidStateException("Worker closed");
            }

            var logTags = workerUpdateableSettings.LogTags ?? Array.Empty<WorkerLogTag>();
            var reqData = new
            {
                LogLevel = (workerUpdateableSettings.LogLevel ?? WorkerLogLevel.None).GetDescription<EnumMemberAttribute>(x=>x.Value!),
                LogTags = logTags.Select(m => m.GetDescription<EnumMemberAttribute>(x=>x.Value!)),
            };

            // Fire and forget
            Channel.RequestAsync(MethodId.WORKER_UPDATE_SETTINGS, null, reqData).ContinueWithOnFaultedHandleLog(Logger);
        }
    }

    /// <summary>
    /// Create a WebRtcServer.
    /// </summary>
    /// <returns></returns>
    public async Task<WebRtcServer.WebRtcServer> CreateWebRtcServerAsync(WebRtcServerOptions webRtcServerOptions)
    {
        Logger.LogDebug("CreateWebRtcServerAsync()");

        using (await CloseLock.ReadLockAsync())
        {
            if (Closed)
            {
                throw new InvalidStateException("Workder closed");
            }
            var reqData = new {
                WebRtcServerId = Guid.NewGuid().ToString(),
                webRtcServerOptions.ListenInfos
            };

            await Channel.RequestAsync(MethodId.WORKER_CREATE_WEBRTC_SERVER, null, reqData);

            var webRtcServer = new WebRtcServer.WebRtcServer(LoggerFactory,
                new WebRtcServerInternal
                {
                    WebRtcServerId = reqData.WebRtcServerId
                },
                Channel,
                webRtcServerOptions.AppData
            );

            lock (WebRtcServersLock)
            {
                WebRtcServers.Add(webRtcServer);
            }

            webRtcServer.On("@close", (_, _) =>
            {
                lock (WebRtcServersLock)
                {
                    WebRtcServers.Remove(webRtcServer);
                }
                return Task.CompletedTask;
            });

            // Emit observer event.
            Observer.Emit("newwebrtcserver", webRtcServer);

            return webRtcServer;
        }
    }

    /// <summary>
    /// Create a Router.
    /// </summary>
    public async Task<Router.Router> CreateRouterAsync(RouterOptions routerOptions)
    {
        Logger.LogDebug("CreateRouterAsync()");

        using (await CloseLock.ReadLockAsync())
        {
            if (Closed)
            {
                throw new InvalidStateException("Workder closed");
            }

            // This may throw.
            var rtpCapabilities = ORTC.Ortc.GenerateRouterRtpCapabilities(routerOptions.MediaCodecs);

            var reqData = new { RouterId = Guid.NewGuid().ToString() };

            await Channel.RequestAsync(MethodId.WORKER_CREATE_ROUTER, null, reqData);

            var router = new Router.Router(LoggerFactory,
                new RouterInternal(reqData.RouterId),
                new RouterData
                {
                    RtpCapabilities = rtpCapabilities
                },
                Channel,
                PayloadChannel,
                routerOptions.AppData);

            lock (RoutersLock)
            {
                Routers.Add(router);
            }

            router.On("@close", (_, _) =>
            {
                lock (RoutersLock)
                {
                    Routers.Remove(router);
                }
                return Task.CompletedTask;
            });

            // Emit observer event.
            Observer.Emit("newrouter", router);

            return router;
        }
    }

    #endregion Request

    #region IDisposable Support

    private bool disposedValue = false; // 要检测冗余调用

    protected virtual void DestoryManaged()
    {

    }

    protected virtual void DestoryUnmanaged()
    {

    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                // TODO: 释放托管状态(托管对象)。
                DestoryManaged();
            }

            // TODO: 释放未托管的资源(未托管的对象)并在以下内容中替代终结器。
            // TODO: 将大型字段设置为 null。
            DestoryUnmanaged();

            disposedValue = true;
        }
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