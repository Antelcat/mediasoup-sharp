using System.Runtime.InteropServices;
using FBS.Notification;
using MediasoupSharp.Channel;
using MediasoupSharp.Internals.Extensions;
using Microsoft.Extensions.Logging;

namespace MediasoupSharp.Worker;

public class WorkerNative : WorkerBase
{
    private readonly string[] argv;

    private readonly string version;

    private readonly IntPtr channelPtr;

    public WorkerNative(ILoggerFactory loggerFactory, MediasoupOptions mediasoupOptions)
        : base(loggerFactory, mediasoupOptions)
    {
        var workerSettings = mediasoupOptions.MediasoupSettings.WorkerSettings;
        var args = new List<string>
        {
            "" // Ignore `workerPath`
        };

        if(workerSettings.LogLevel.HasValue)
        {
            args.Add($"--logLevel={workerSettings.LogLevel.Value.GetEnumMemberValue()}");
        }

        if(!workerSettings.LogTags.IsNullOrEmpty())
        {
            foreach (var logTag in workerSettings.LogTags)
            {
                args.Add($"--logTag={logTag.GetEnumMemberValue()}");
            }
        }

        if(workerSettings.RtcMinPort.HasValue)
        {
            args.Add($"--rtcMinPort={workerSettings.RtcMinPort}");
        }

        if(workerSettings.RtcMaxPort.HasValue)
        {
            args.Add($"--rtcMaxPort={workerSettings.RtcMaxPort}");
        }

        if(!workerSettings.DtlsCertificateFile.IsNullOrWhiteSpace())
        {
            args.Add($"--dtlsCertificateFile={workerSettings.DtlsCertificateFile}");
        }

        if(!workerSettings.DtlsPrivateKeyFile.IsNullOrWhiteSpace())
        {
            args.Add($"--dtlsPrivateKeyFile={workerSettings.DtlsPrivateKeyFile}");
        }

        if(!workerSettings.LibwebrtcFieldTrials.IsNullOrWhiteSpace())
        {
            args.Add($"--libwebrtcFieldTrials={workerSettings.LibwebrtcFieldTrials}");
        }
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
        args.Add(null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

        argv    = args.ToArray();
        version = mediasoupOptions.MediasoupStartupSettings.MediasoupVersion;

        var threadId = Environment.CurrentManagedThreadId;

        Channel                =  new ChannelNative(LoggerFactory.CreateLogger<ChannelNative>(), threadId);
        Channel.OnNotification += OnNotificationHandle;
        channelPtr              =  GCHandle.ToIntPtr(GCHandle.Alloc(Channel, GCHandleType.Normal));
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
            if(workerRunResult == 42)
            {
                Logger.LogError("OnExit() | Worker run failed due to wrong settings");
                Emit("@failure", new Exception("Worker run failed due to wrong settings"));
            }
            else if(workerRunResult == 0)
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

    public override Task CloseAsync()
    {
        throw new NotImplementedException();
    }

    protected override void DestroyUnmanaged()
    {
        if (channelPtr == IntPtr.Zero) return;
        var handle = GCHandle.FromIntPtr(channelPtr);
        if(handle.IsAllocated)
        {
            handle.Free();
        }
    }

    #region Event handles

    private void OnNotificationHandle(string handlerId, Event @event, Notification notification)
    {
        if(@event != Event.WORKER_RUNNING)
        {
            return;
        }

        Channel.OnNotification -= OnNotificationHandle;
        Emit("@success");
    }

    #endregion
}