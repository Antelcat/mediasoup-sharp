using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using FBS.Notification;
using Antelcat.LibuvSharp;
using Antelcat.MediasoupSharp.Exceptions;
using Antelcat.MediasoupSharp.Internals.Extensions;
using Antelcat.MediasoupSharp.Settings;
using Microsoft.Extensions.Logging;

namespace Antelcat.MediasoupSharp.Worker;

/// <summary>
/// A worker represents a mediasoup C++ subprocess that runs in a single CPU core and handles Router instances.
/// </summary>
public class Worker : WorkerBase
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
    public override int Pid { get; }

    /// <summary>
    /// Is spawn done?
    /// </summary>
    private bool spawnDone;

    /// <summary>
    /// Pipes.
    /// </summary>
    private readonly UVStream?[] pipes;

    #endregion Private Fields

    /// <summary>
    /// <para>Events:</para>
    /// <para>@emits died - (error: Error)</para>
    /// <para>@emits @success</para>
    /// <para>@emits @failure - (error: Error)</para>
    /// <para>Observer events:</para>
    /// <para>@emits close</para>
    /// <para>@emits newrouter - (router: Router)</para>
    /// </summary>
    public Worker(ILoggerFactory loggerFactory, MediasoupOptions mediasoupOptions)
        : base(loggerFactory, mediasoupOptions)
    {
        var workerPath = "";
        if (workerPath.IsNullOrWhiteSpace())
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
                          Architecture.X86   => "x86",
                          Architecture.Arm   => "arm",
                          Architecture.Arm64 => "arm64",
                          _                  => throw new NotSupportedException("Unsupported architecture")
                      };
            var location  = Assembly.GetEntryAssembly()!.Location;
            var directory = Path.GetDirectoryName(location)!;
            workerPath = Path.Combine(directory, "runtimes", rid, "native", "mediasoup-worker");
        }

        var workerSettings = mediasoupOptions.WorkerSettings!;

        var env = new[] { $"MEDIASOUP_VERSION={Mediasoup.Version.ToString()}" };

        var argv = new List<string> { workerPath };
        if(workerSettings.LogLevel.HasValue)
        {
            argv.Add($"--logLevel={workerSettings.LogLevel.Value.GetEnumText()}");
        }

        if(!workerSettings.LogTags.IsNullOrEmpty())
        {
            argv.AddRange(workerSettings.LogTags.Select(logTag => $"--logTag={logTag.GetEnumText()}"));
        }

        if(workerSettings.RtcMinPort.HasValue)
        {
            argv.Add($"--rtcMinPort={workerSettings.RtcMinPort}");
        }

        if(workerSettings.RtcMaxPort.HasValue)
        {
            argv.Add($"--rtcMaxPort={workerSettings.RtcMaxPort}");
        }

        if(!workerSettings.DtlsCertificateFile.IsNullOrWhiteSpace())
        {
            argv.Add($"--dtlsCertificateFile={workerSettings.DtlsCertificateFile}");
        }

        if(!workerSettings.DtlsPrivateKeyFile.IsNullOrWhiteSpace())
        {
            argv.Add($"--dtlsPrivateKeyFile={workerSettings.DtlsPrivateKeyFile}");
        }

        if(!workerSettings.LibwebrtcFieldTrials.IsNullOrWhiteSpace())
        {
            argv.Add($"--libwebrtcFieldTrials={workerSettings.LibwebrtcFieldTrials}");
        }

        Logger.LogDebug("Worker() | Spawning worker process: {Arguments}", string.Join(" ", argv));

        pipes = new UVStream[StdioCount];

        // fd 0 (stdin)   : Just ignore it.
        // fd 1 (stdout)  : Pipe it for 3rd libraries that log their own stuff.
        // fd 2 (stderr)  : Same as stdout.
        // fd 3 (channel) : Producer Channel fd.
        // fd 4 (channel) : Consumer Channel fd.
        for(var i = 1; i < StdioCount; i++)
        {
            var pipe = pipes[i]      =  new Pipe { Writeable = true, Readable = true };
            pipe.Data += data =>
            {
                var str = Encoding.UTF8.GetString(data);
                if (str.Contains("throwing")) Logger.LogError(str);
                else Logger.LogInformation(str);
            };
        }

        try
        {
            // 和 Node.js 不同，_child 没有 error 事件。不过，Process.Spawn 可抛出异常。
            child = Process.Spawn(
                new ProcessOptions
                {
                    File        = workerPath,
                    Arguments   = argv.ToArray(),
                    Environment = env,
                    Detached    = false,
                    Streams     = pipes!,
                },
                OnExit
            );


            Pid = child.Id;
        }
        catch(Exception ex)
        {
            child = null;
            CloseAsync().ConfigureAwait(false).GetAwaiter().GetResult();

            if(!spawnDone)
            {
                spawnDone = true;
                Logger.LogError(ex, $"{nameof(Worker)}() | Worker process failed [pid:{{ProcessId}}]", Pid);
                Emit("@failure", ex);
            }
            else
            {
                // 执行到这里的可能性？
                Logger.LogError(ex, $"{nameof(Worker)}() | Worker process error [pid:{{ProcessId}}]", Pid);
                Emit("died", ex);
            }
        }

        Channel = new Channel.Channel(LoggerFactory.CreateLogger<Channel.Channel>(),
            pipes[3]!,
            pipes[4]!, 
            Pid);
        
        Channel.OnNotification += OnNotificationHandle;

        foreach (var pipe in pipes)
        {
            pipe?.Resume();
        }
    }

    public override async Task CloseAsync()
    {
        Logger.LogDebug("CloseAsync() | Worker[{ProcessId}]", Pid);

        await using(await CloseLock.WriteLockAsync())
        {
            if (Closed)
            {
                throw new InvalidStateException("Worker closed");
            }

            Closed = true;

            // Kill the worker process.
            if(child != null)
            {
                // Remove event listeners but leave a fake 'error' handler to avoid
                // propagation.
                child.Kill(
                    15 /*SIGTERM*/
                );
                child = null;
            }

            // Close the Channel instance.
            await Channel.CloseAsync();

            // Close every Router.
            Router.Router[] routersForClose;
            lock(RoutersLock)
            {
                routersForClose = Routers.ToArray();
                Routers.Clear();
            }

            foreach(var router in routersForClose)
            {
                await router.WorkerClosedAsync();
            }

            // Close every WebRtcServer.
            WebRtcServer.WebRtcServer[] webRtcServersForClose;
            lock(WebRtcServersLock)
            {
                webRtcServersForClose = WebRtcServers.ToArray();
                WebRtcServers.Clear();
            }

            foreach(var webRtcServer in webRtcServersForClose)
            {
                await webRtcServer.WorkerClosedAsync();
            }

            // Emit observer event.
            Observer.Emit("close");
        }
    }

    protected override void DestroyManaged()
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
        Channel.OnNotification -= OnNotificationHandle;
    }

    private void OnExit(Process process)
    {
        // If killed by ourselves, do nothing.
        if(!process.IsAlive)
        {
            return;
        }

        child = null;
        CloseAsync().ConfigureAwait(false).GetAwaiter().GetResult();

        if(!spawnDone)
        {
            spawnDone = true;

            if(process.ExitCode == 42)
            {
                Logger.LogError("OnExit() | Worker process failed due to wrong settings [pid:{ProcessId}]", Pid);
                Emit("@failure", new Exception($"Worker process failed due to wrong settings [pid:{Pid}]"));
            }
            else
            {
                Logger.LogError("OnExit() | Worker process failed unexpectedly [pid:{ProcessId}, code:{ExitCode}, signal:{TermSignal}]", Pid, process.ExitCode, process.TermSignal);
                Emit("@failure",
                    new Exception(
                        $"Worker process failed unexpectedly [pid:{Pid}, code:{process.ExitCode}, signal:{process.TermSignal}]"
                    )
                );
            }
        }
        else
        {
            Logger.LogError("OnExit() | Worker process failed unexpectedly [pid:{ProcessId}, code:{ExitCode}, signal:{TermSignal}]", Pid, process.ExitCode, process.TermSignal);
            Emit("died",
                new Exception(
                    $"Worker process died unexpectedly [pid:{Pid}, code:{process.ExitCode}, signal:{process.TermSignal}]"
                )
            );
        }
    }

    #endregion Event handles
}