using System.Runtime.InteropServices;
using Antelcat.AutoGen.ComponentModel.Diagnostic;
using Antelcat.LibuvSharp;
using Antelcat.MediasoupSharp.FBS.Notification;
using Antelcat.MediasoupSharp.FBS.Request;
using Antelcat.MediasoupSharp.FBS.Transport;
using Antelcat.MediasoupSharp.FBS.Worker;
using Antelcat.MediasoupSharp.Internals.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Threading;
using Body = Antelcat.MediasoupSharp.FBS.Request.Body;

namespace Antelcat.MediasoupSharp;

[AutoExtractInterface(
    NamingTemplate = nameof(IWorker),
    Interfaces = [typeof(IDisposable)],
    Exclude = [nameof(Dispose)])]
public class WorkerImpl<TWorkerAppData> : EnhancedEventEmitter<WorkerEvents>, IWorker<TWorkerAppData>
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

    #endregion Private Fields

    #region Protected Fields

    /// <summary>
    /// Logger.
    /// </summary>
    private readonly ILogger logger = new Logger<WorkerImpl<TWorkerAppData>>();

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
    protected readonly AsyncReaderWriterLock CloseLock = new(null);

    #endregion Protected Fields

    /// <summary>
    /// Custom app data.
    /// </summary>
    public TWorkerAppData AppData { get; set; }

    /// <summary>
    /// Observer instance.
    /// </summary>
    public WorkerObserver Observer { get; } = new();

    /// <summary>
    /// <para>Events : <see cref="WorkerEvents"/></para>
    /// <para>Observer events : <see cref="WorkerObserverEvents"/></para>
    /// </summary>
    public WorkerImpl(WorkerSettings<TWorkerAppData> workerSettings)
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


        logger.LogDebug("Worker() | Spawning worker process: {Arguments}", string.Join(" ", argv));

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
                logger.LogError(ex, "Worker() | Worker process failed [pid:{ProcessId}]", Pid);
                this.Emit(static x => x.failure, ex);
            }
            else
            {
                // 执行到这里的可能性？
                logger.LogError(ex, "Worker() | Worker process error [pid:{ProcessId}]", Pid);
                this.SafeEmit(static x => x.Died, ex);
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

                logger.LogDebug("worker process running [pid:{Pid}]", Pid);

                this.Emit(static x => x.success);
            }
        });

        child.Closed += () =>
        {
            logger.LogDebug(
                "worker subprocess closed [pid:{Pid}, code:{Code}, signal:{Signal}]",
                Pid,
                tmpChild.ExitCode,
                tmpChild.TermSignal
            );


            this.SafeEmit(static x => x.SubprocessClose);
        };

        channel.OnNotification += OnNotificationHandle;

        foreach (var pipe in pipes)
        {
            pipe?.Resume();
        }
        
        HandleListenerError();
    }

    #region Request

    /// <summary>
    /// Dump Worker.
    /// </summary>
    public async Task<Antelcat.MediasoupSharp.FBS.Worker.DumpResponseT> DumpAsync()
    {
        logger.LogDebug("DumpAsync()");

        await using (await CloseLock.ReadLockAsync())
        {
            if (Closed)
            {
                throw new InvalidStateException("Worker closed");
            }

            var response = await channel.RequestAsync(static _ => null, Method.WORKER_DUMP);
            var data     = response.NotNull().BodyAsWorker_DumpResponse().UnPack();
            return data;
        }
    }

    /// <summary>
    /// Get mediasoup-worker process resource usage.
    /// </summary>
    public async Task<Antelcat.MediasoupSharp.FBS.Worker.ResourceUsageResponseT> GetResourceUsageAsync()
    {
        logger.LogDebug("GetResourceUsageAsync()");

        await using (await CloseLock.ReadLockAsync())
        {
            if (Closed)
            {
                throw new InvalidStateException("Worker closed");
            }

            var response = await channel.RequestAsync(static _ => null, Method.WORKER_GET_RESOURCE_USAGE);
            var data     = response.NotNull().BodyAsWorker_ResourceUsageResponse().UnPack();

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
    public async Task<WebRtcServerImpl<TWebRtcServerAppData>> CreateWebRtcServerAsync<TWebRtcServerAppData>(
        WebRtcServerOptions<TWebRtcServerAppData> webRtcServerOptions)
        where TWebRtcServerAppData : new()
    {
        logger.LogDebug("CreateWebRtcServerAsync()");

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

            await channel.RequestAsync(bufferBuilder => 
                    CreateWebRtcServerRequest.Pack(bufferBuilder,
                    new CreateWebRtcServerRequestT
                    {
                        WebRtcServerId = webRtcServerId,
                        ListenInfos    = fbsListenInfos
                    }).Value, Method.WORKER_CREATE_WEBRTCSERVER,
                Body.Worker_CreateWebRtcServerRequest
            );

            var webRtcServer = new WebRtcServerImpl<TWebRtcServerAppData>(
                new WebRtcServerInternal { WebRtcServerId = webRtcServerId },
                channel,
                webRtcServerOptions.AppData
            );

            lock (webRtcServersLock)
            {
                WebRtcServers.Add(webRtcServer);
            }

            webRtcServer.On(static x => x.close,
                () =>
                {
                    lock (webRtcServersLock) WebRtcServers.Remove(webRtcServer);

                    return Task.CompletedTask;
                }
            );

            // Emit observer event.
            Observer.SafeEmit(static x => x.NewWebrtcServer, webRtcServer);

            return webRtcServer;
        }
    }

    /// <summary>
    /// Create a Router.
    /// </summary>
    /// <returns>Router</returns>
    public async Task<RouterImpl<TRouterAppData>> CreateRouterAsync<TRouterAppData>(
        RouterOptions<TRouterAppData> routerOptions)
        where TRouterAppData : new()
    {
        logger.LogDebug("CreateRouterAsync()");

        await using (await CloseLock.ReadLockAsync())
        {
            if (Closed)
            {
                throw new InvalidStateException("Worker closed");
            }

            // This may throw.
            var rtpCapabilities = Ortc.GenerateRouterRtpCapabilities(routerOptions.MediaCodecs);

            var routerId = Guid.NewGuid().ToString();

            await channel.RequestAsync(bufferBuilder =>
                    CreateRouterRequest.Pack(bufferBuilder,
                        new CreateRouterRequestT
                        {
                            RouterId = routerId
                        }).Value,
                Method.WORKER_CREATE_ROUTER,
                Body.Worker_CreateRouterRequest);

            var router = new RouterImpl<TRouterAppData>(
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

            router.On(static x => x.close,
                () =>
                {
                    lock (routersLock) routers.Remove(router);
                }
            );

            // Emit observer event.
            Observer.SafeEmit(static x => x.NewRouter, router);

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

    ~WorkerImpl() => Dispose(false);

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
        logger.LogDebug("CloseAsync() | Worker[{ProcessId}]", Pid);

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
            Observer.SafeEmit(static x => x.Close);
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
        logger.LogDebug("Worker[{ProcessId}] process running", Pid);
        this.Emit(static x => x.success);
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
                logger.LogError("OnExit() | Worker process failed due to wrong settings [pid:{ProcessId}]", Pid);
                this.Emit(static x => x.failure,
                    new Exception($"Worker process failed due to wrong settings [pid:{Pid}]"));
            }
            else
            {
                logger.LogError(
                    "OnExit() | Worker process failed unexpectedly [pid:{ProcessId}, code:{ExitCode}, signal:{TermSignal}]",
                    Pid, process.ExitCode, process.TermSignal);
                this.Emit(static x => x.failure,
                    new Exception(
                        $"Worker process failed unexpectedly [pid:{Pid}, code:{process.ExitCode}, signal:{process.TermSignal}]"
                    )
                );
            }
        }
        else
        {
            logger.LogError(
                "OnExit() | Worker process failed unexpectedly [pid:{ProcessId}, code:{ExitCode}, signal:{TermSignal}]",
                Pid, process.ExitCode, process.TermSignal);
            this.SafeEmit(static x => x.Died,
                new Exception(
                    $"Worker process died unexpectedly [pid:{Pid}, code:{process.ExitCode}, signal:{process.TermSignal}]"
                )
            );
        }
    }

    private void HandleListenerError() =>
        this.On(static x => x.ListenerError, tuple =>
        {
            logger.LogError(tuple.error,
                "event listener threw an error [eventName:{EventName}]:",
                tuple.eventName);
        });

    #endregion Event handles
}