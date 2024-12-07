using System.Reflection;
using System.Text.Json.Serialization;
using Antelcat.LibuvSharp;
using Antelcat.MediasoupSharp.Internals.Converters;
using Antelcat.MediasoupSharp.Internals.Extensions;
using Microsoft.Extensions.Logging;

namespace Antelcat.MediasoupSharp;

using Observer = IEnhancedEventEmitter<ObserverEvents>;

public abstract class ObserverEvents
{
    public required IWorker NewWorker;
}

public partial class Mediasoup
{
    private Mediasoup() { }
    
    private static readonly ILogger<Mediasoup> Logger = new Logger<Mediasoup>(); 
    public static Version Version { get; } = Version.Parse((string)typeof(Mediasoup)
        .Assembly
        .CustomAttributes
        .First(static x => x.AttributeType == typeof(AssemblyFileVersionAttribute))
        .ConstructorArguments.First().Value.NotNull());

    /// <summary>
    /// Observer instance.
    /// </summary>
    public static Observer Observer { get; } = new EnhancedEventEmitter<ObserverEvents>();

    public static Task<WorkerImpl<TWorkerAppData>>[] CreateWorkers<TWorkerAppData>(
        WorkerSettings<TWorkerAppData> settings, int numWorkers) where TWorkerAppData : new()
    {
        var sources = new TaskCompletionSource<WorkerImpl<TWorkerAppData>>[numWorkers];

        var index = 0;
        
        for (var i = 0; i < numWorkers; i++) sources[i] = new();
        
        Queue(() =>
        {
            for (var i = 0; i < numWorkers; i++)
            {
                var worker = new WorkerImpl<TWorkerAppData>(settings);
                worker.On(static x => x.success, async _ =>
                    {
                        Observer.SafeEmit(static x => x.NewWorker, worker);
                        await Task.Delay(1);
                        lock (sources)
                        {
                            var source = sources[index++];
                            source.SetResult(worker);
                        }
                    })
                    .On(static x => x.failure, ex =>
                    {
                        lock (sources)
                        {
                            var source = sources[index++];
                            source.SetException(ex.NotNull());
                        }
                    });
            }
        });
        return sources.Select(x => x.Task).ToArray();
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
            debugLogEmitter.On(static x => x.DebugLog, x => listeners.OnDebug(x.Item1, x.Item2));
        }

        if (listeners?.OnWarn != null)
        {
            warnLogEmitter = new EnhancedEventEmitter<LoggerEmitterEvents>();
            warnLogEmitter.On(static x => x.WarnLog, x => listeners.OnWarn(x.Item1, x.Item2));
        }

        if (listeners?.OnError != null)
        {
            errorLogEmitter = new EnhancedEventEmitter<LoggerEmitterEvents>();
            errorLogEmitter.On(static x => x.ErrorLog, x => listeners.OnError(x.Item1, x.Item2, x.Item3));
        }

        MediasoupSharp.Logger.DebugLogEmitter = debugLogEmitter;
        MediasoupSharp.Logger.WarnLogEmitter  = warnLogEmitter;
        MediasoupSharp.Logger.DebugLogEmitter = errorLogEmitter;
    }

    public static IReadOnlyCollection<JsonConverter> JsonConverters => IEnumStringConverter.JsonConverters;
}

partial class Mediasoup
{
    private static void Queue(Action action)
    {
        var loop = Loop.Default;
        if (loop.IsRunning) throw new OperationCanceledException("loop is already running");
        lock (loop)
        {
            if (loop.IsRunning) throw new OperationCanceledException("loop is already running");
            ThreadPool.QueueUserWorkItem(_ =>
            {
                if (!loop.Run(action))
                {
                    throw new OperationCanceledException("loop run failed");
                }
            });
        }
    }

}