using System.Reflection;
using System.Text.Json.Serialization;
using Antelcat.LibuvSharp;
using Antelcat.MediasoupSharp.RtpParameters;
using Antelcat.MediasoupSharp.EnhancedEvent;
using Antelcat.MediasoupSharp.Internals.Converters;
using Antelcat.MediasoupSharp.Internals.Extensions;
using Antelcat.MediasoupSharp.Logger;
using Antelcat.MediasoupSharp.Settings;
using Antelcat.MediasoupSharp.Worker;
using Microsoft.Extensions.Logging;

namespace Antelcat.MediasoupSharp;

public partial class Mediasoup
{
    private static readonly ILogger<Mediasoup> Logger = new Logger.Logger<Mediasoup>(); 
    public static Version Version { get; } = Version.Parse((string)typeof(Mediasoup)
        .Assembly
        .CustomAttributes
        .First(static x => x.AttributeType == typeof(AssemblyFileVersionAttribute))
        .ConstructorArguments.First().Value!);

    /// <summary>
    /// Observer instance.
    /// </summary>
    public static EnhancedEventEmitter Observer { get; } = new();
  
    public static async IAsyncEnumerable<Worker.Worker> CreateWorkersAsync(MediasoupOptions workerSettings)
    {
        var num = workerSettings.NumWorkers;
        if (num is not > 0) throw new ArgumentException("Num workers should be > 0");

        var sources = new TaskCompletionSource<Worker.Worker>[num.Value];
        
        for (var i = 0; i < num.Value; i++) sources[i] = new();
        Queue(() =>
        {
            for (var i = 0; i < num.Value; i++)
            {
                var worker = new WorkerProcess(workerSettings);
                worker.On("@success", async () =>
                    {
                        Observer.Emit("newworker", worker);
                        await Task.Delay(1);
                        lock (sources)
                        {
                            foreach (var source in sources)
                            {
                                if (source.Task.IsCompleted) continue;
                                source.SetResult(worker);
                            }
                        }
                    })
                    .On("@failure", () =>
                    {
                        lock (sources)
                        {
                            foreach (var source in sources)
                            {
                                if (source.Task.IsCompleted) continue;
                                source.SetException(new Exception("Worker create failed"));
                            }
                        }
                    });
            }
        });

        foreach (var source in sources)
        {
#pragma warning disable VSTHRD003
            yield return await source.Task;
#pragma warning restore VSTHRD003
        }
    }

    /// <summary>
    /// Get a cloned copy of the mediasoup supported RTP capabilities.
    /// </summary>
    public static RtpCapabilities GetSupportedRtpCapabilities() => RtpCapabilities.SupportedRtpCapabilities.DeepClone();
    public class LogEventListeners
    {
        public Action<string, string>?            OnDebug;
        public Action<string, string>?            OnWarn;
        public Action<string, string, Exception>? OnError;
    }

    public static void SetLogEventListeners(LogEventListeners? listeners)
    {
        Logger.LogDebug($"{nameof(SetLogEventListeners)}()");

        EnhancedEventEmitter<LoggerEmitterEvents>? debugLogEmitter = null;
        EnhancedEventEmitter<LoggerEmitterEvents>? warnLogEmitter  = null;
        EnhancedEventEmitter<LoggerEmitterEvents>? errorLogEmitter = null;

        if (listeners?.OnDebug != null)
        {
            debugLogEmitter = new EnhancedEventEmitter<LoggerEmitterEvents>();
            debugLogEmitter.On(static x => x.debuglog, x => listeners.OnDebug(x.Item1, x.Item2));
        }

        if (listeners?.OnWarn != null)
        {
            warnLogEmitter = new EnhancedEventEmitter<LoggerEmitterEvents>();
            warnLogEmitter.On(static x => x.warnlog, x => listeners.OnWarn(x.Item1, x.Item2));
        }

        if (listeners?.OnError != null)
        {
            errorLogEmitter = new EnhancedEventEmitter<LoggerEmitterEvents>();
            errorLogEmitter.On(static x => x.errorlog, x => listeners.OnError(x.Item1, x.Item2, x.Item3));
        }

        MediasoupSharp.Logger.Logger.DebugLogEmitter = debugLogEmitter;
        MediasoupSharp.Logger.Logger.WarnLogEmitter  = warnLogEmitter;
        MediasoupSharp.Logger.Logger.DebugLogEmitter = errorLogEmitter;
    }

    public static IReadOnlyCollection<JsonConverter> JsonConverters => IEnumStringConverter.JsonConverters;
}

partial class Mediasoup
{
    private static void Queue(Action action)
    {
        ThreadPool.QueueUserWorkItem(_ =>
        {
            var loop = Loop.Default;
            while (loop.IsRunning)
            {
            }

            if (!loop.Run(action))
            {
                throw new OperationCanceledException("Loop failed");
            }
        });
    }
}