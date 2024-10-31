using System.Runtime.InteropServices;
using Antelcat.MediasoupSharp.Channel;
using FBS.Notification;
using Antelcat.MediasoupSharp.Internals.Extensions;
using Antelcat.MediasoupSharp.Settings;
using Microsoft.Extensions.Logging;

namespace Antelcat.MediasoupSharp.Worker;

public class WorkerNative : Worker
{
    private readonly string[] argv;

    private readonly string version;

    private readonly IntPtr channelPtr;

    public WorkerNative(MediasoupOptions mediasoupOptions) : base(mediasoupOptions)
    {
        NativeLibrary.SetDllImportResolver(typeof(LibMediasoupWorkerNative).Assembly,(name, assembly, path) =>
        {
            var file = WorkerFile;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && Path.GetExtension(file) != ".exe")
            {
                file += ".exe";
            }

            if (!Path.IsPathRooted(file))
            {
                file = Path.GetFullPath(file, AppContext.BaseDirectory);
            }
            if (!File.Exists(file))
            {
                throw new FileNotFoundException($"could not file worker:{file}");
            }

            return NativeLibrary.Load(file);
        });
        
        var workerSettings = mediasoupOptions.WorkerSettings!;
        var args = new List<string?>
        {
            "" // Ignore `workerPath`
        };

        if (workerSettings.LogLevel.HasValue)
        {
            args.Add($"--logLevel={workerSettings.LogLevel.Value.GetEnumText()}");
        }

        if (!workerSettings.LogTags.IsNullOrEmpty())
        {
            foreach (var logTag in workerSettings.LogTags)
            {
                args.Add($"--logTag={logTag.GetEnumText()}");
            }
        }

#pragma warning disable CS0618 // 类型或成员已过时
        if (workerSettings.RtcMinPort.HasValue)
        {
            args.Add($"--rtcMinPort={workerSettings.RtcMinPort}");
        }

        if (workerSettings.RtcMaxPort.HasValue)
        {
            args.Add($"--rtcMaxPort={workerSettings.RtcMaxPort}");
        }
#pragma warning restore CS0618 // 类型或成员已过时

        if (!workerSettings.DtlsCertificateFile.IsNullOrWhiteSpace())
        {
            args.Add($"--dtlsCertificateFile={workerSettings.DtlsCertificateFile}");
        }

        if (!workerSettings.DtlsPrivateKeyFile.IsNullOrWhiteSpace())
        {
            args.Add($"--dtlsPrivateKeyFile={workerSettings.DtlsPrivateKeyFile}");
        }

        if (!workerSettings.LibwebrtcFieldTrials.IsNullOrWhiteSpace())
        {
            args.Add($"--libwebrtcFieldTrials={workerSettings.LibwebrtcFieldTrials}");
        }

        args.Add(null);

        argv    = args.ToArray()!;
        version = Mediasoup.Version.ToString();

        var threadId = Environment.CurrentManagedThreadId;

        Channel                =  new ChannelNative(threadId);
        Channel.OnNotification += OnNotificationHandle;
        channelPtr             =  GCHandle.ToIntPtr(GCHandle.Alloc(Channel, GCHandleType.Normal));
    }

    public void Run()
    {
        var workerRunResult = LibMediasoupWorkerNative.MediasoupWorkerRun(
            argv.Length - 1,
            argv,
            version,
            0,
            0,
            0,
            0,
            ChannelNative.OnChannelRead,
            channelPtr,
            ChannelNative.OnChannelWrite,
            channelPtr
        );

        void OnExit()
        {
            switch (workerRunResult)
            {
                case 42:
                    Logger.LogError("OnExit() | Worker run failed due to wrong settings");
                    Emit("@failure", new Exception("Worker run failed due to wrong settings"));
                    break;
                case 0:
                    Logger.LogError("OnExit() | Worker died unexpectedly");
                    Emit("died", new Exception("Worker died unexpectedly"));
                    break;
                default:
                    Logger.LogError("OnExit() | Worker run failed unexpectedly");
                    Emit("@failure", new Exception("Worker run failed unexpectedly"));
                    break;
            }
        }

        OnExit();
    }

    public override int Pid => throw new NotSupportedException();

    public override Task CloseAsync() => throw new NotSupportedException();

    protected override void DestroyUnmanaged()
    {
        if (channelPtr == IntPtr.Zero) return;
        var handle = GCHandle.FromIntPtr(channelPtr);
        if (handle.IsAllocated)
        {
            handle.Free();
        }
    }

    #region Event handles

    private void OnNotificationHandle(string handlerId, Event @event, Notification notification)
    {
        if (@event != Event.WORKER_RUNNING)
        {
            return;
        }

        Channel.OnNotification -= OnNotificationHandle;
        Emit("@success");
    }

    #endregion
}