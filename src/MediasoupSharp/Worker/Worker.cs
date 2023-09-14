using System.Collections;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using LibuvSharp;
using MediasoupSharp.Errors;
using MediasoupSharp.ORTC;
using MediasoupSharp.Router;
using MediasoupSharp.WebRtcServer;
using Microsoft.Extensions.Logging;
using Process = LibuvSharp.Process;

namespace MediasoupSharp.Worker;

public interface IWorker
{

    void Close();
    Task<IRouter> CreateRouter<TRouterAppData>(RouterOptions<TRouterAppData> options);
}

public interface IWorker<TWorkerAppData> : IWorker
{
}

/// <summary>
/// A worker represents a mediasoup C++ subprocess that runs in a single CPU core and handles Router instances.
/// </summary>
internal partial class Worker<TWorkerAppData>
    : EnhancedEventEmitter<WorkerEvents>, IWorker<TWorkerAppData>
{
    private readonly ILogger? logger;

    /// <summary>
    /// mediasoup-worker child process.
    /// </summary>
    private Process? child;

    private readonly Channel.Channel channel;

    private readonly PayloadChannel.PayloadChannel payloadChannel;

    public bool Closed { get; private set; }

    public bool Died { get; private set; }

    public object AppData { get; set; }

    private readonly HashSet<WebRtcServer.WebRtcServer> webRtcServers = new();

    private readonly HashSet<IRouter> routers = new();

    private EnhancedEventEmitter<WorkerObserverEvents> Observer { get; }

    private ILogger? workerLogger;


    public Worker(WorkerSettings<TWorkerAppData> mediasoupOptions, ILoggerFactory? loggerFactory = null)
    {
        logger       = loggerFactory?.CreateLogger(GetType());
        workerLogger = loggerFactory?.CreateLogger("Worker");
        var logLevel             = mediasoupOptions.LogLevel;
        var logTags              = mediasoupOptions.LogTags;
        var rtcMinPort           = mediasoupOptions.RtcMinPort;
        var rtcMaxPort           = mediasoupOptions.RtcMaxPort;
        var dtlsCertificateFile  = mediasoupOptions.DtlsCertificateFile;
        var dtlsPrivateKeyFile   = mediasoupOptions.DtlsPrivateKeyFile;
        var libwebrtcFieldTrials = mediasoupOptions.LibwebrtcFieldTrials;
        var appData              = mediasoupOptions.AppData;

        Observer = new EnhancedEventEmitter<WorkerObserverEvents>(loggerFactory);

        var spawnBin  = WorkerBin;
        var spawnArgs = new List<string>();

        if (Env("MEDIASOUP_USE_VALGRIND") is "true")
        {
            spawnBin = Env("MEDIASOUP_VALGRIND_BIN") ?? "valgrind";
            var options = Env("MEDIASOUP_VALGRIND_OPTIONS");
            if (!options.IsNullOrEmpty())
            {
                spawnArgs = spawnArgs.Concat(MyRegex().Matches(options!).Select(x => x.Value)).ToList();
            }

            spawnArgs.Add(WorkerBin);
        }

        if (logLevel.HasValue)
        {
            spawnArgs.Add($"--{nameof(logLevel)}={logLevel}");
        }

        foreach (var logTag in logTags ?? new())
        {
            spawnArgs.Add($"--{nameof(logTag)}={logTag}");
        }

        if (rtcMinPort.HasValue)
        {
            spawnArgs.Add($"--{nameof(rtcMinPort)}={rtcMinPort}");
        }

        if (rtcMaxPort.HasValue)
        {
            spawnArgs.Add($"--{nameof(rtcMaxPort)}={rtcMaxPort}");
        }

        if (!dtlsCertificateFile.IsNullOrEmpty())
        {
            spawnArgs.Add($"--{nameof(dtlsCertificateFile)}={dtlsCertificateFile}");
        }

        if (!dtlsPrivateKeyFile.IsNullOrEmpty())
        {
            spawnArgs.Add($"--{nameof(dtlsPrivateKeyFile)}={dtlsPrivateKeyFile}");
        }

        if (!libwebrtcFieldTrials.IsNullOrEmpty())
        {
            spawnArgs.Add($"--{nameof(libwebrtcFieldTrials)}={libwebrtcFieldTrials}");
        }

        logger?.LogDebug("Worker() | Spawning worker process: {} {}", spawnBin, string.Join(" ", spawnArgs));

        var pipes = new Pipe[7];
        
        //NativeHandle的起始指针不可为空，只要不管他就行，如果不读不写会引发ENOTSUP
        pipes[0] = new Pipe { Writeable = false, Readable = true };

        // fd 0 (stdin)   : Just ignore it. 
        // fd 1 (stdout)  : Pipe it for 3rd libraries that log their own stuff.
        // fd 2 (stderr)  : Same as stdout.
        // fd 3 (channel) : Producer Channel fd.
        // fd 4 (channel) : Consumer Channel fd.
        // fd 5 (channel) : Producer PayloadChannel fd.
        // fd 6 (channel) : Consumer PayloadChannel fd.
        for (var i = 1; i < pipes.Length; i++)
        {
            var pipe = pipes[i] = new Pipe { Writeable = true, Readable = true };
            pipe.Data += _ =>
            {
                Debugger.Break();
            };
            pipe.Error += _ =>
            {
                Debugger.Break();
            };
            pipe.Complete += () =>
            {
                Debugger.Break();
            };
            pipe.Closed += () =>
            {
                Debugger.Break();
            };
            pipe.Drain += () =>
            {
                Debugger.Break();
            };
        }

        var pOptions = new ProcessOptions
        {
            File      = WorkerBin,
            Arguments = spawnArgs.ToArray(),
            Environment = (from DictionaryEntry pair
                        in Environment.GetEnvironmentVariables()
                    select $"{pair.Key}={pair.Value}")
                .Append($"MEDIASOUP_VERSION={MediasoupSharp.Version}")
                .ToArray(),
            Detached = false,
            Streams  = pipes,
        };
        child = Process.Spawn(pOptions, _ => { Debugger.Break(); });

        Pid = child.ID;

        channel = new Channel.Channel(
            pipes[3],
            pipes[4], 
            Pid, 
            loggerFactory);

        payloadChannel = new PayloadChannel.PayloadChannel(
            pipes[5],
            pipes[6],
            loggerFactory);

        AppData = appData ?? typeof(TWorkerAppData).New<TWorkerAppData>()!;

        var spawnDone = false;

        channel.Once($"{Pid}", async args =>
        {
            var @event = args![0];
            if (!spawnDone && @event is "running")
            {
                spawnDone = true;
                logger?.LogDebug("worker process running [pid:{Id}]", Pid);
                await Emit("@success");
            }
        });

        channel.On("exit", async args =>
        {
            var code   = (int?)args![0];
            var signal = (string?)args[0];
            child = null;

            if (!spawnDone)
            {
                spawnDone = true;

                if (code is 42)
                {
                    logger?.LogDebug("worker process failed due to wrong settings [pid:{Id}]", Pid);
                    Close();
                    await Emit("@failure", new TypeError("wrong settings"));
                }
                else
                {
                    logger?.LogDebug("worker process failed unexpectedly [pid:{Id}]", Pid);
                    Close();
                    await Emit("@failure",
                        new TypeError($"[pid:${Pid}, code:{code}, signal:{signal}]"));
                }
            }
            else
            {
                logger?.LogDebug("worker process died unexpectedly [pid:{Id}]", Pid);
                WorkerDied(new TypeError($"[pid:${Pid}, code:{code}, signal:{signal}]"));
            }
        });
        channel.On($"error", async (args) =>
        {
            var error = (Exception)args![0];
            child = null;

            if (!spawnDone)
            {
                spawnDone = true;
                logger?.LogDebug("worker process failed [pid:{Id}]: {E}", Pid, error.Message);
                Close();
                await Emit("@failure", error);
            }
            else
            {
                logger?.LogDebug("worker process error [pid:{Id}]: {E}", Pid, error.Message);
                WorkerDied(error);
            }
        });

        // Be ready for 3rd party worker libraries logging to stdout.
        pipes[1].Data += buffer =>
        {
            foreach (var line in Encoding.UTF8.GetString(buffer).Split("\n"))
            {
                if (!line.IsNullOrEmpty())
                {
                    workerLogger?.LogDebug("(stdout) {Line}", line);
                }
            }
        };

        // In case of a worker bug, mediasoup will log to stderr.
        pipes[2].Data += buffer =>
        {
            foreach (var line in Encoding.UTF8.GetString(buffer).Split("\n"))
            {
                if (!line.IsNullOrEmpty())
                {
                    workerLogger?.LogError("(stderr) {Line}", line);
                }
            }
        };
    }

    /// <summary>
    /// Worker process PID.
    /// </summary>
    public int Pid { get; }

    public HashSet<WebRtcServer.WebRtcServer> WebRtcServersForTesting => webRtcServers;

    public HashSet<IRouter> RoutersForTesting => routers;

    public void Close()
    {
        if (Closed)
        {
            throw new InvalidStateError("Worker closed");
        }

        logger?.LogDebug("CloseAsync() | Worker");


        Closed = true;

        // Kill the worker process.
        if (child != null)
        {
            // Remove event listeners but leave a fake "error" hander to avoid
            // propagation.
            child.Kill(15 /*SIGTERM*/);
            child = null;
        }

        // Close the Channel instance.
        channel.Close();

        // Close the PayloadChannel instance.
        payloadChannel.Close();

        // Close every Router.
        foreach (var router in routers)
        {
            router.WorkerClosed();
        }

        routers.Clear();

        // Close every WebRtcServer.
        foreach (var webRtcServer in webRtcServers)
        {
            webRtcServer.WorkerClosed();
        }

        webRtcServers.Clear();

        // Emit observer event.
        _ = Observer.SafeEmit("close");
    }

    public async Task<object> DumpAsync()
    {
        logger?.LogDebug("dump()");

        return await channel.Request("worker.dump");
    }

    public async Task<WorkerResourceUsage> GetResourceUsage()
    {
        logger?.LogDebug("getResourceUsage()");

        return (WorkerResourceUsage)(await channel.Request("worker.getResourceUsage"))!;
    }

    public async Task UpdateSettings<TTWorkerAppData>(WorkerUpdateableSettings<TWorkerAppData> settings)
    {
        await channel.Request("worker.updateSettings", null, settings);
    }

    public async Task<WebRtcServer<TWebRtcServerAppData>> CreateWebRtcServer<TWebRtcServerAppData>(
        WebRtcServerOptions<TWebRtcServerAppData> options)
    {
        var listenInfos = options.ListenInfos;
        var appData     = options.AppData;
        logger?.LogDebug("createWebRtcServer()");

        var reqData = new
        {
            webRtcServerId = Guid.NewGuid().ToString(),
            listenInfos
        };

        await channel.Request("worker.createWebRtcServer", null, reqData);

        var webRtcServer = new WebRtcServer<TWebRtcServerAppData>(
            new WebRtcServerInternal()
            {
                WebRtcServerId = reqData.webRtcServerId
            },
            channel,
            appData
        );

        webRtcServers.Add(webRtcServer);
        webRtcServer.On("@close", async _ => webRtcServers.Remove(webRtcServer));

        // Emit observer event.
        _ = Observer.SafeEmit("newwebrtcserver", webRtcServer);

        return webRtcServer;
    }


    /// <summary>
    /// Create a Router.
    /// </summary>
    /// <returns></returns>
    public async Task<IRouter> CreateRouter<TRouterAppData>(RouterOptions<TRouterAppData> options)
    {
        var mediaCodecs = options.MediaCodecs;
        var appData     = options.AppData;

        logger?.LogDebug("createRouter()");

        // This may throw.
        var rtpCapabilities = Ortc.GenerateRouterRtpCapabilities(mediaCodecs);

        var reqData = new { routerId = Guid.NewGuid().ToString() };

        await channel.Request("worker.createRouter", null, reqData);

        var data = new RouterData { RtpCapabilities = rtpCapabilities };
        var router = new Router<TRouterAppData>(
            new RouterInternal
            {
                RouterId = reqData.routerId
            },
            data,
            channel,
            payloadChannel,
            appData);

        routers.Add(router);
        router.On("@close", async _ => routers.Remove(router));

        // Emit observer event.
        await Observer.SafeEmit("newrouter", router);

        return router;
    }

    
    private void WorkerDied(Exception exception)
    {
        if (Closed)
        {
            return;
        }

        logger?.LogDebug("died() [error:${Error}]", exception);

        Closed = true;
        Died   = true;

        // Close the Channel instance.
        channel.Close();

        // Close the PayloadChannel instance.
        payloadChannel.Close();

        // Close every Router.
        foreach (var router in routers)
        {
            router.WorkerClosed();
        }

        routers.Clear();

        // Close every WebRtcServer.
        foreach (var webRtcServer in webRtcServers)
        {
            webRtcServer.WorkerClosed();
        }

        webRtcServers.Clear();

        _ = SafeEmit("died", exception);

        // Emit observer event.
        _ = Observer.SafeEmit("close");
    }

    [GeneratedRegex("/\\s+/")]
    private static partial Regex MyRegex();
}