using System.Runtime.InteropServices;
using Antelcat.MediasoupSharp.Channel;
using FBS.Notification;
using Antelcat.MediasoupSharp.Internals.Extensions;
using Antelcat.MediasoupSharp.Settings;
using Microsoft.Extensions.Logging;

namespace Antelcat.MediasoupSharp.Worker;

public class WorkerNative : WorkerBase
{
    private readonly string[] argv;

    private readonly string version;

    private readonly IntPtr channelPtr;

    public WorkerNative(MediasoupOptions mediasoupOptions)
        : base(mediasoupOptions)
    {
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

        if (workerSettings.RtcMinPort.HasValue)
        {
            args.Add($"--rtcMinPort={workerSettings.RtcMinPort}");
        }

        if (workerSettings.RtcMaxPort.HasValue)
        {
            args.Add($"--rtcMaxPort={workerSettings.RtcMaxPort}");
        }

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
            if (workerRunResult == 42)
            {
                Logger.LogError("OnExit() | Worker run failed due to wrong settings");
                Emit("@failure", new Exception("Worker run failed due to wrong settings"));
            }
            else if (workerRunResult == 0)
            {
                Logger.LogError("OnExit() | Worker died unexpectedly");
                Emit("died", new Exception("Worker died unexpectedly"));
            }
            else
            {
                Logger.LogError("OnExit() | Worker run failed unexpectedly");
                Emit("@failure", new Exception("Worker run failed unexpectedly"));
            }
        }

        OnExit();
    }

    public override int Pid => throw new NotSupportedException();

    public override Task CloseAsync()
    {
        throw new NotImplementedException();
    }

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