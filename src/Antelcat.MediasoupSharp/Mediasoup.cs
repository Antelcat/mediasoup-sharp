using System.Diagnostics;
using System.Reflection;
using Antelcat.MediasoupSharp.RtpParameters;
using Antelcat.MediasoupSharp;
using Antelcat.MediasoupSharp.Settings;
using Antelcat.MediasoupSharp.Worker;
using Force.DeepCloner;
using Microsoft.Extensions.Logging;

namespace Antelcat.MediasoupSharp;

public class Mediasoup
{
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
    public static EnhancedEvent.EnhancedEventEmitter Observer { get; } = new();

    public static Task<WorkerBase> CreateWorkerAsync(ILoggerFactory loggerFactory, MediasoupOptions workerSettings)
    {
        var source = new TaskCompletionSource<WorkerBase>();
        var worker = new Worker.Worker(loggerFactory, workerSettings);
        worker
            .On("@success", () =>
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
    public static RtpCapabilities GetSupportedRtpCapabilities()
    {
        return RtpCapabilities.SupportedRtpCapabilities.DeepClone();
    }

    /// <summary>
    /// Get next mediasoup Worker.
    /// </summary>
    public IWorker GetWorker()
    {
        workersLock.EnterReadLock();
        try
        {
            if(nextMediasoupWorkerIndex > workers.Count - 1)
            {
                throw new Exception("None worker");
            }

            if(++nextMediasoupWorkerIndex == workers.Count)
            {
                nextMediasoupWorkerIndex = 0;
            }

            return workers[nextMediasoupWorkerIndex];
        }
        catch(Exception ex)
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
        catch(Exception ex)
        {
            Debug.WriteLine($"Add worker failure: {ex.Message}");
            throw;
        }
        finally
        {
            workersLock.ExitWriteLock();
        }
    }
}