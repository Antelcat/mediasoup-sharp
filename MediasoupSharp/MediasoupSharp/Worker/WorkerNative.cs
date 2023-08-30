using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using MediasoupSharp.Channel;
using MediasoupSharp.PayloadChannel;

namespace MediasoupSharp.Worker
{
    public class WorkerNative : WorkerBase
    {
        private readonly string[] argv;
        private readonly string version;
        private readonly IntPtr channelPtr;
        private readonly IntPtr payloadChannelPtr;

        public WorkerNative(ILoggerFactory loggerFactory, MediasoupOptions mediasoupOptions) : base(loggerFactory,
            mediasoupOptions)
        {
            var workerSettings = mediasoupOptions.MediasoupSettings.WorkerSettings;
            var argv = new List<string>
            {
                "" // Ignore `workerPath`
            };
            if (workerSettings.LogLevel.HasValue)
            {
                argv.Add($"--logLevel={workerSettings
                    .LogLevel
                    .Value
                    .GetDescription<EnumMemberAttribute>(x=>x.Value)}");
            }

            if (!workerSettings.LogTags.IsNullOrEmpty())
            {
                workerSettings.LogTags!
                    .ForEach(m => 
                        argv.Add($"--logTag={
                            m.GetDescription<EnumMemberAttribute>(x=>x.Value)}"));
            }

            if (workerSettings.RtcMinPort.HasValue)
            {
                argv.Add($"--rtcMinPort={workerSettings.RtcMinPort}");
            }

            if (workerSettings.RtcMaxPort.HasValue)
            {
                argv.Add($"--rtcMaxPort={workerSettings.RtcMaxPort}");
            }

            if (!workerSettings.DtlsCertificateFile.IsNullOrWhiteSpace())
            {
                argv.Add($"--dtlsCertificateFile={workerSettings.DtlsCertificateFile}");
            }

            if (!workerSettings.DtlsPrivateKeyFile.IsNullOrWhiteSpace())
            {
                argv.Add($"--dtlsPrivateKeyFile={workerSettings.DtlsPrivateKeyFile}");
            }
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            argv.Add(null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

            this.argv = argv.ToArray();
            version = mediasoupOptions.MediasoupStartupSettings.MediasoupVersion;

            var threadId = Environment.CurrentManagedThreadId;

            _channel = new ChannelNative(_loggerFactory.CreateLogger<ChannelNative>(), threadId);
            _channel.MessageEvent += OnChannelMessage;
            channelPtr = GCHandle.ToIntPtr(GCHandle.Alloc(_channel, GCHandleType.Normal));

            _payloadChannel = new PayloadChannelNative(_loggerFactory.CreateLogger<PayloadChannelNative>(), threadId);
            payloadChannelPtr = GCHandle.ToIntPtr(GCHandle.Alloc(_payloadChannel, GCHandleType.Normal));
        }
        void Solve(int code)
        {
            switch (code)
            {
                case 42:
                    _logger.LogError($"OnExit() | Worker run failed due to wrong settings");
                    Emit("@failure", new Exception("Worker run failed due to wrong settings"));
                    break;
                case 0:
                    _logger.LogError($"OnExit() | Worker died unexpectedly");
                    Emit("died", new Exception("Worker died unexpectedly"));
                    break;
                default:
                    _logger.LogError($"OnExit() | Worker run failed unexpectedly");
                    Emit("@failure", new Exception("Worker run failed unexpectedly"));
                    break;
            }

            File.WriteAllText("./CRASHLOG.txt", $"{code}");
        }
        public async Task RunAsync()
        {
            var info = new ProcessStartInfo
            {
                FileName = "./mediasoup-worker.exe",
                WorkingDirectory = ".",
                Environment = { { "MEDIASOUP_VERSION", "0.0.1" } },
                RedirectStandardError = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
            };
            var l = argv.ToList();
            l.RemoveAll(x => x.IsNullOrWhiteSpace());
            l.ForEach(info.ArgumentList.Add);
            var cancel = new CancellationTokenSource();
            var process = new Process
            {
                StartInfo = info
            };
            process.Exited += (o, e) =>
            {
                Solve(process.ExitCode);
            };
            AppDomain.CurrentDomain.ProcessExit += (o, e) =>
            {
                process.Kill(); 
            };

            if (!process.Start())
            {
                throw new ExternalException();
            }
            _logger.LogDebug("Current process id {ProcessId}", Environment.ProcessId);
            _logger.LogDebug("Sub process id {ProcessId}", process.Id);
            await process.WaitForExitAsync(cancel.Token);
        }

        public override Task CloseAsync() => throw new NotImplementedException();

        protected void DestroyUnmanaged()
        {
            if (channelPtr != IntPtr.Zero)
            {
                var handle = GCHandle.FromIntPtr(channelPtr);
                if (handle.IsAllocated)
                {
                    handle.Free();
                }
            }

            if (payloadChannelPtr != IntPtr.Zero)
            {
                var handle = GCHandle.FromIntPtr(payloadChannelPtr);
                if (handle.IsAllocated)
                {
                    handle.Free();
                }
            }
        }

        #region Event handles

        private void OnChannelMessage(string targetId, string @event, string? data)
        {
            if (@event != "running")
            {
                return;
            }

            _channel.MessageEvent -= OnChannelMessage;
            Emit("@success");
        }

        #endregion
    }
}