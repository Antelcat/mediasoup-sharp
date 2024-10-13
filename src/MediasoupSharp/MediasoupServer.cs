using System.Diagnostics;
using Force.DeepCloner;
using MediasoupSharp.RtpParameters;
using MediasoupSharp.Worker;

namespace MediasoupSharp;

public class MediasoupServer
{
    private readonly List<IWorker> workers = [];

    private int nextMediasoupWorkerIndex;

    private readonly ReaderWriterLockSlim workersLock = new();

    /// <summary>
    /// Observer instance.
    /// </summary>
    public EventEmitter.EventEmitter Observer { get; } = new();

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
