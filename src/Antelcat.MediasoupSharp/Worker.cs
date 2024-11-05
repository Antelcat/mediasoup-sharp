using System.Runtime.InteropServices;
using Antelcat.AutoGen.ComponentModel.Diagnostic;
using Antelcat.LibuvSharp;
using Antelcat.MediasoupSharp.Internals.Extensions;
using FBS.Notification;
using FBS.Request;
using FBS.Transport;
using FBS.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Threading;
using Body = FBS.Request.Body;

namespace Antelcat.MediasoupSharp;

using WorkerObserver = EnhancedEventEmitter<WorkerObserverEvents>;

public enum WorkerLogLevel
{
    /// <summary>
    /// Log all severities.
    /// </summary>
    Debug,

    /// <summary>
    /// Log “warn” and “error” severities.
    /// </summary>
    Warn,

    /// <summary>
    /// Log “error” severity.
    /// </summary>
    Error,

    /// <summary>
    /// Do not log anything.
    /// </summary>
    None
}

public enum WorkerLogTag
{
    /// <summary>
    /// Logs about software/library versions, configuration and process information.
    /// </summary>
    Info,

    /// <summary>
    /// Logs about ICE.
    /// </summary>
    Ice,

    /// <summary>
    /// Logs about DTLS.
    /// </summary>
    Dtls,

    /// <summary>
    /// Logs about RTP.
    /// </summary>
    Rtp,

    /// <summary>
    /// Logs about SRTP encryption/decryption.
    /// </summary>
    Srtp,

    /// <summary>
    /// Logs about RTCP.
    /// </summary>
    Rtcp,

    /// <summary>
    /// Logs about RTP retransmission, including NACK/PLI/FIR.
    /// </summary>
    Rtx,

    /// <summary>
    /// Logs about transport bandwidth estimation.
    /// </summary>
    Bwe,

    /// <summary>
    /// Logs related to the scores of Producers and Consumers.
    /// </summary>
    Score,

    /// <summary>
    /// Logs about video simulcast.
    /// </summary>
    Simulcast,

    /// <summary>
    /// Logs about video SVC.
    /// </summary>
    Svc,

    /// <summary>
    /// Logs about SCTP (DataChannel).
    /// </summary>
    Sctp,

    /// <summary>
    /// Logs about messages (can be SCTP messages or direct messages).
    /// </summary>
    Message,
}

public record WorkerSettings<TWorkerAppData>
{
    public string?         WorkerFile          { get; set; }
    public string?         DtlsCertificateFile { get; set; }
    public string?         DtlsPrivateKeyFile  { get; set; }
    public WorkerLogLevel? LogLevel            { get; set; } = WorkerLogLevel.Warn;
    public WorkerLogTag[]  LogTags             { get; set; } = [];

    /// <summary>
    /// Minimum RTC port for ICE, DTLS, RTP, etc. Default 10000.
    /// </summary>
    [Obsolete("Use |PortRange| in TransportListenInfo object instead.")]
    public int? RtcMinPort { get; set; }

    /// <summary>
    /// Maximum RTC port for ICE, DTLS, RTP, etc. Default 59999.
    /// </summary>
    [Obsolete("Use |PortRange| in TransportListenInfo object instead.")]
    public int? RtcMaxPort { get; set; }

    public string? LibwebrtcFieldTrials { get; set; }

    public bool? DisableLiburing { get; set; }

    public TWorkerAppData? AppData { get; set; }
}

public class WorkerEvents
{
    public Exception?           died;
    public object?              subprocessclose;
    public (string, Exception)? listenererror;

    // Private events.
    public object?    _success;
    public Exception? _failure;
}

public class WorkerObserverEvents
{
    public object?       close;
    public IWebRtcServer newwebrtcserver;
    public IRouter?      newrouter;
}

[AutoExtractInterface(Interfaces =
    [
        typeof(IEnhancedEventEmitter<WorkerEvents>),
        typeof(IDisposable)
    ],
    Exclude = [nameof(Dispose)])]
public class Worker<TWorkerAppData> : EnhancedEventEmitter<WorkerEvents>, IWorker
    where TWorkerAppData : new()
{
    #region Constants

    private const int StdioCount = 5;

    #endregion Constants

    #region Private Fields

    /// <summary>
    /// mediasoup-worker child process.
    /// </summary>
    private Process? child;

    /// <summary>
    /// Worker process PID.
    /// </summary>
    public int Pid { get; }

    /// <summary>
    /// Is spawn done?
    /// </summary>
    private bool spawnDone;

    /// <summary>
    /// Pipes.
    /// </summary>
    private readonly UVStream?[] pipes;

    private bool subprocessClosed;

    #endregion Private Fields

    #region Protected Fields

    /// <summary>
    /// Logger.
    /// </summary>
    protected readonly ILogger Logger = new Logger<Worker<TWorkerAppData>>();

    /// <summary>
    /// Channel instance.
    /// </summary>
    private readonly Channel channel;

    /// <summary>
    /// Router set.
    /// </summary>
    private readonly List<IRouter> routers = [];

    /// <summary>
    /// _routers locker.
    /// </summary>
    private readonly object routersLock = new();

    /// <summary>
    /// WebRtcServers set.
    /// </summary>
    public readonly List<IWebRtcServer> WebRtcServers = [];

    /// <summary>
    /// _webRtcServer locker.
    /// </summary>
    private readonly object webRtcServersLock = new();

    /// <summary>
    /// Closed flag.
    /// </summary>
    public bool Closed { get; private set; }

    /// <summary>
    /// Close locker.
    /// </summary>
    protected readonly AsyncReaderWriterLock CloseLock = new();

    #endregion Protected Fields


    /// <summary>
    /// Custom app data.
    /// </summary>
    public TWorkerAppData AppData { get; }

    /// <summary>
    /// Observer instance.
    /// </summary>
    public WorkerObserver Observer { get; } = new();

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
    public Worker(WorkerSettings<TWorkerAppData> workerSettings)
    {
        AppData = workerSettings.AppData ?? new();

        var workerFile = workerSettings.WorkerFile;
        if (string.IsNullOrWhiteSpace(workerFile))
        {
            var rid = (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
                          ? "linux"
                          : RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
                              ? "osx"
                              : RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                                  ? "win"
                                  : throw new NotSupportedException("Unsupported platform")) + '-' +
                      RuntimeInformation.OSArchitecture switch
                      {
                          Architecture.X64   => "x64",
                          Architecture.Arm64 => "arm64",
                          //mediasoup only provides x64/arm64 binary see:https://github.com/versatica/mediasoup/releases
                          _ => throw new NotSupportedException("Unsupported architecture")
                      };
            workerFile = Path.Combine("runtimes", rid, "native", "mediasoup-worker");
        }
        // If absolute path, change to relative path for libuv
        else if (Path.IsPathRooted(workerFile))
        {
            workerFile = Path.GetRelativePath(AppContext.BaseDirectory, workerFile);
        }

        var workerFile1 = workerFile;

        var env = new[] { $"MEDIASOUP_VERSION={Mediasoup.Version.ToString()}" };

        var argv = new List<string> { workerFile1 };
        if (workerSettings.LogLevel.HasValue)
        {
            argv.Add($"--logLevel={workerSettings.LogLevel.Value.GetEnumText()}");
        }

        if (!workerSettings.LogTags.IsNullOrEmpty())
        {
            argv.AddRange(workerSettings.LogTags.Select(logTag => $"--logTag={logTag.GetEnumText()}"));
        }

#pragma warning disable CS0618 // 类型或成员已过时
        if (workerSettings.RtcMinPort.HasValue)
        {
            argv.Add($"--rtcMinPort={workerSettings.RtcMinPort}");
        }

        if (workerSettings.RtcMaxPort.HasValue)
        {
            argv.Add($"--rtcMaxPort={workerSettings.RtcMaxPort}");
        }
#pragma warning restore CS0618 // 类型或成员已过时

        if (!workerSettings.DtlsCertificateFile.IsNullOrWhiteSpace())
        {
            argv.Add($"--dtlsCertificateFile={workerSettings.DtlsCertificateFile}");
        }

        if (!workerSettings.DtlsPrivateKeyFile.IsNullOrWhiteSpace())
        {
            argv.Add($"--dtlsPrivateKeyFile={workerSettings.DtlsPrivateKeyFile}");
        }

        if (!workerSettings.LibwebrtcFieldTrials.IsNullOrWhiteSpace())
        {
            argv.Add($"--libwebrtcFieldTrials={workerSettings.LibwebrtcFieldTrials}");
        }

        if (workerSettings.DisableLiburing is true)
        {
            argv.Add("--disableLiburing=true");
        }


        Logger.LogDebug("Worker() | Spawning worker process: {Arguments}", string.Join(" ", argv));

        pipes = new UVStream[StdioCount];

        // fd 0 (stdin)   : Just ignore it.
        // fd 1 (stdout)  : Pipe it for 3rd libraries that log their own stuff.
        // fd 2 (stderr)  : Same as stdout.
        // fd 3 (channel) : Producer Channel fd.
        // fd 4 (channel) : Consumer Channel fd.
        for (var i = 1; i < StdioCount; i++)
        {
            pipes[i] = new Pipe { Writeable = true, Readable = true };
        }

        Process tmpChild;
        try
        {
            // 和 Node.js 不同，_child 没有 error 事件。不过，Process.Spawn 可抛出异常。
            tmpChild = child = Process.Spawn(new ProcessOptions
                {
                    File                    = workerFile1,
                    Arguments               = argv.ToArray(),
                    Environment             = env,
                    Detached                = false,
                    Streams                 = pipes!,
                    CurrentWorkingDirectory = AppContext.BaseDirectory
                },
                OnExit
            );


            Pid = child.Id;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            
            child = null;
            CloseAsync().ConfigureAwait(false).GetAwaiter().GetResult();

            if (!spawnDone)
            {
                spawnDone = true;
                Logger.LogError(ex, "Worker() | Worker process failed [pid:{ProcessId}]", Pid);
                Emit("@failure", ex);
            }
            else
            {
                // 执行到这里的可能性？
                Logger.LogError(ex, "Worker() | Worker process error [pid:{ProcessId}]", Pid);
                Emit("died", ex);
            }

            return;
        }

        channel = new Channel(
            pipes[3]!,
            pipes[4]!,
            Pid);


        channel.Once($"{Pid}", (Event @event) =>
        {
            if (!spawnDone && @event == Event.WORKER_RUNNING)
            {
                spawnDone = true;

                Logger.LogDebug("worker process running [pid:{Pid}]", Pid);

                Emit("@success");
            }
        });

        child.Closed += () =>
        {
            Logger.LogDebug(
                "worker subprocess closed [pid:{Pid}, code:{Code}, signal:{Signal}]",
                Pid,
                tmpChild.ExitCode,
                tmpChild.TermSignal
            );

            subprocessClosed = true;

            Emit("subprocessclose");
        };

        channel.OnNotification += OnNotificationHandle;

        foreach (var pipe in pipes)
        {
            pipe?.Resume();
        }
    }

    #region Request

    /// <summary>
    /// Dump Worker.
    /// </summary>
    public async Task<FBS.Worker.DumpResponseT> DumpAsync()
    {
        Logger.LogDebug("DumpAsync()");

        await using (await CloseLock.ReadLockAsync())
        {
            if (Closed)
            {
                throw new InvalidStateException("Worker closed");
            }


            // Build Request
            var bufferBuilder = channel.BufferPool.Get();

            var response = await channel.RequestAsync(bufferBuilder, Method.WORKER_DUMP);
            var data     = response.Value.BodyAsWorker_DumpResponse().UnPack();
            return data;
        }
    }

    /// <summary>
    /// Get mediasoup-worker process resource usage.
    /// </summary>
    public async Task<FBS.Worker.ResourceUsageResponseT> GetResourceUsageAsync()
    {
        Logger.LogDebug("GetResourceUsageAsync()");

        await using (await CloseLock.ReadLockAsync())
        {
            if (Closed)
            {
                throw new InvalidStateException("Worker closed");
            }

            // Build Request
            var bufferBuilder = channel.BufferPool.Get();

            var response = await channel.RequestAsync(bufferBuilder, Method.WORKER_GET_RESOURCE_USAGE);
            var data     = response.Value.BodyAsWorker_ResourceUsageResponse().UnPack();

            return data;
        }
    }

    /// <summary>
    /// Updates the worker settings in runtime. Just a subset of the worker settings can be updated.
    /// </summary>
    /*public async Task UpdateSettingsAsync(WorkerUpdateableSettings workerUpdateableSettings)
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
    }*/

    /// <summary>
    /// Create a WebRtcServer.
    /// </summary>
    /// <returns>WebRtcServer</returns>
    public async Task<WebRtcServer<TWebRtcServerAppData>> CreateWebRtcServerAsync<TWebRtcServerAppData>(
        WebRtcServerOptions<TWebRtcServerAppData> webRtcServerOptions)
        where TWebRtcServerAppData : new()
    {
        Logger.LogDebug("CreateWebRtcServerAsync()");

        await using (await CloseLock.ReadLockAsync())
        {
            if (Closed)
            {
                throw new InvalidStateException("Worker closed");
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
            var bufferBuilder = channel!.BufferPool.Get();

            var createWebRtcServerRequestT = new CreateWebRtcServerRequestT
            {
                WebRtcServerId = webRtcServerId,
                ListenInfos    = fbsListenInfos
            };

            var createWebRtcServerRequestOffset =
                CreateWebRtcServerRequest.Pack(bufferBuilder, createWebRtcServerRequestT);

            await channel.RequestAsync(bufferBuilder, Method.WORKER_CREATE_WEBRTCSERVER,
                Body.Worker_CreateWebRtcServerRequest,
                createWebRtcServerRequestOffset.Value
            );

            var webRtcServer = new WebRtcServer<TWebRtcServerAppData>(
                new WebRtcServerInternal { WebRtcServerId = webRtcServerId },
                channel,
                webRtcServerOptions.AppData
            );

            lock (webRtcServersLock)
            {
                WebRtcServers.Add(webRtcServer);
            }

            webRtcServer.On(
                "@close",
                () =>
                {
                    lock (webRtcServersLock)
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
    public async Task<Router<TRouterAppData>> CreateRouterAsync<TRouterAppData>(
        RouterOptions<TRouterAppData> routerOptions)
        where TRouterAppData : new()
    {
        Logger.LogDebug("CreateRouterAsync()");

        await using (await CloseLock.ReadLockAsync())
        {
            if (Closed)
            {
                throw new InvalidStateException("Worker closed");
            }

            // This may throw.
            var rtpCapabilities = Ortc.GenerateRouterRtpCapabilities(routerOptions.MediaCodecs);

            var routerId = Guid.NewGuid().ToString();

            // Build Request
            var bufferBuilder = channel.BufferPool.Get();

            var createRouterRequestT = new CreateRouterRequestT
            {
                RouterId = routerId
            };

            var createRouterRequestOffset = CreateRouterRequest.Pack(bufferBuilder, createRouterRequestT);

            await channel.RequestAsync(bufferBuilder, Method.WORKER_CREATE_ROUTER,
                Body.Worker_CreateRouterRequest,
                createRouterRequestOffset.Value);

            var router = new Router<TRouterAppData>(
                new RouterInternal
                {
                    RouterId = routerId
                },
                new RouterData { RtpCapabilities = rtpCapabilities },
                channel,
                routerOptions.AppData
            );

            lock (routersLock)
            {
                routers.Add(router);
            }

            router.On(
                "@close",
                () =>
                {
                    lock (routersLock)
                    {
                        routers.Remove(router);
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

    private void Dispose(bool disposing)
    {
        if (disposedValue) return;
        if (disposing)
        {
            DestroyManaged();
        }

        disposedValue = true;
    }

    ~Worker() => Dispose(false);

    // 添加此代码以正确实现可处置模式。
    public void Dispose()
    {
        // 请勿更改此代码。将清理代码放入以上 Dispose(bool disposing) 中。
        Dispose(true);
        // TODO: 如果在以上内容中替代了终结器，则取消注释以下行。
        GC.SuppressFinalize(this);
    }

    #endregion IDisposable Support

    public async Task CloseAsync()
    {
        Logger.LogDebug("CloseAsync() | Worker[{ProcessId}]", Pid);

        await using (await CloseLock.WriteLockAsync())
        {
            if (Closed)
            {
                throw new InvalidStateException("Worker closed");
            }

            Closed = true;

            // Kill the worker process.
            if (child != null)
            {
                // Remove event listeners but leave a fake 'error' handler to avoid
                // propagation.
                child.Kill(
                    15 /*SIGTERM*/
                );
                child = null;
            }

            // Close the Channel instance.
            await channel.CloseAsync();

            // Close every Router.
            IRouter[] routersForClose;
            lock (routersLock)
            {
                routersForClose = routers.ToArray();
                routers.Clear();
            }

            foreach (var router in routersForClose)
            {
                await router.WorkerClosedAsync();
            }

            // Close every WebRtcServer.
            IWebRtcServer[] webRtcServersForClose;
            lock (webRtcServersLock)
            {
                webRtcServersForClose = WebRtcServers.ToArray();
                WebRtcServers.Clear();
            }

            foreach (var webRtcServer in webRtcServersForClose)
            {
                await webRtcServer.WorkerClosedAsync();
            }

            // Emit observer event.
            Observer.Emit("close");
        }
    }

    private void DestroyManaged()
    {
        child?.Dispose();
        foreach (var pipe in pipes)
        {
            pipe?.Dispose();
        }
    }

    #region Event handles

    private void OnNotificationHandle(string handlerId, Event @event, Notification notification)
    {
        if (spawnDone || @event != Event.WORKER_RUNNING) return;
        spawnDone = true;
        Logger.LogDebug("Worker[{ProcessId}] process running", Pid);
        Emit("@success");
        channel.OnNotification -= OnNotificationHandle;
    }

    private void OnExit(Process process)
    {
        // If killed by ourselves, do nothing.
        if (!process.IsAlive)
        {
            return;
        }

        child = null;
        CloseAsync().ConfigureAwait(false).GetAwaiter().GetResult();

        if (!spawnDone)
        {
            spawnDone = true;

            if (process.ExitCode == 42)
            {
                Logger.LogError("OnExit() | Worker process failed due to wrong settings [pid:{ProcessId}]", Pid);
                Emit("@failure", new Exception($"Worker process failed due to wrong settings [pid:{Pid}]"));
            }
            else
            {
                Logger.LogError(
                    "OnExit() | Worker process failed unexpectedly [pid:{ProcessId}, code:{ExitCode}, signal:{TermSignal}]",
                    Pid, process.ExitCode, process.TermSignal);
                Emit("@failure",
                    new Exception(
                        $"Worker process failed unexpectedly [pid:{Pid}, code:{process.ExitCode}, signal:{process.TermSignal}]"
                    )
                );
            }
        }
        else
        {
            Logger.LogError(
                "OnExit() | Worker process failed unexpectedly [pid:{ProcessId}, code:{ExitCode}, signal:{TermSignal}]",
                Pid, process.ExitCode, process.TermSignal);
            Emit("died",
                new Exception(
                    $"Worker process died unexpectedly [pid:{Pid}, code:{process.ExitCode}, signal:{process.TermSignal}]"
                )
            );
        }
    }

    #endregion Event handles
}