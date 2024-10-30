using System.Diagnostics;
using System.Reflection;
using System.Text.Json.Serialization;
using Antelcat.MediasoupSharp.RtpParameters;
using Antelcat.MediasoupSharp;
using Antelcat.MediasoupSharp.EnhancedEvent;
using Antelcat.MediasoupSharp.Internals.Converters;
using Antelcat.MediasoupSharp.Logger;
using Antelcat.MediasoupSharp.Settings;
using Antelcat.MediasoupSharp.Worker;
using Force.DeepCloner;
using Microsoft.Extensions.Logging;

namespace Antelcat.MediasoupSharp;

public class Mediasoup
{
    private static readonly ILogger<Mediasoup> logger = new Logger.Logger<Mediasoup>(); 
    public static Version Version { get; } = Version.Parse((string)typeof(Mediasoup)
        .Assembly
        .CustomAttributes
        .First(static x => x.AttributeType == typeof(AssemblyFileVersionAttribute))?
        .ConstructorArguments.First().Value!);


    private readonly List<IWorker> workers = [];

    private int nextMediasoupWorkerIndex;

    private readonly ReaderWriterLockSlim workersLock = new();

    /// <summary>
    /// Observer instance.
    /// </summary>
    public static EnhancedEventEmitter Observer { get; } = new();

    public static Task<WorkerBase> CreateWorkerAsync(MediasoupOptions workerSettings)
    {
        var source = new TaskCompletionSource<WorkerBase>();
        var worker = new Worker.Worker(workerSettings);
        worker.On("@success", () =>
            {
                Observer.Emit("newworker", worker);
                source.SetResult(worker);
            })
            .On("@failure", () => source.SetException(new Exception("Worker create failed")));
        return source.Task;
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
        logger.LogDebug($"{nameof(SetLogEventListeners)}()");

        EnhancedEventEmitter<LoggerEmitterEvents>? debugLogEmitter = null;
        EnhancedEventEmitter<LoggerEmitterEvents>? warnLogEmitter  = null;
        EnhancedEventEmitter<LoggerEmitterEvents>? errorLogEmitter = null;

        if (listeners?.OnDebug != null)
        {
            debugLogEmitter = new EnhancedEventEmitter<LoggerEmitterEvents>();
            debugLogEmitter.On(x => x.debuglog, x => listeners.OnDebug(x.Item1, x.Item2));
        }

        if (listeners?.OnWarn != null)
        {
            warnLogEmitter = new EnhancedEventEmitter<LoggerEmitterEvents>();
            warnLogEmitter.On(x => x.warnlog, x => listeners.OnWarn(x.Item1, x.Item2));
        }

        if (listeners?.OnError != null)
        {
            errorLogEmitter = new EnhancedEventEmitter<LoggerEmitterEvents>();
            errorLogEmitter.On(x => x.errorlog, x => listeners.OnError(x.Item1, x.Item2, x.Item3));
        }

        Logger.Logger.DebugLogEmitter = debugLogEmitter;
        Logger.Logger.WarnLogEmitter  = warnLogEmitter;
        Logger.Logger.DebugLogEmitter = errorLogEmitter;
    }

    /// <summary>
    /// Get next mediasoup Worker.
    /// </summary>
    public IWorker GetWorker()
    {
        workersLock.EnterReadLock();
        try
        {
            if (nextMediasoupWorkerIndex > workers.Count - 1)
            {
                throw new Exception("None worker");
            }

            if (++nextMediasoupWorkerIndex == workers.Count)
            {
                nextMediasoupWorkerIndex = 0;
            }

            return workers[nextMediasoupWorkerIndex];
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Get worker failure: {ex.Message}");
            throw;
        }
        finally
        {
            workersLock.ExitReadLock();
        }
    }

    /// <summary>
    /// Add worker.
    /// </summary>
    public void AddWorker(IWorker worker)
    {
        ArgumentNullException.ThrowIfNull(worker);

        workersLock.EnterWriteLock();
        try
        {
            workers.Add(worker);

            // Emit observer event.
            Observer.Emit("newworker", worker);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Add worker failure: {ex.Message}");
            throw;
        }
        finally
        {
            workersLock.ExitWriteLock();
        }
    }

    public static IReadOnlyCollection<JsonConverter> JsonConverters => IEnumStringConverter.JsonConverters;
}