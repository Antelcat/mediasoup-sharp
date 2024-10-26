using Antelcat.MediasoupSharp.Router;
using Antelcat.MediasoupSharp.Settings;
using Antelcat.NodeSharp.Events;
using FBS.Worker;

namespace Antelcat.MediasoupSharp.Worker;

public interface IWorker : IEventEmitter, IDisposable
{
    public int Pid     { get; }
    AppData       AppData { get; }

    EnhancedEvent.EnhancedEventEmitter Observer { get; }

    Task CloseAsync();

    Task<Router.Router> CreateRouterAsync(RouterOptions routerOptions);

    Task<DumpResponseT> DumpAsync();

    Task<ResourceUsageResponseT> GetResourceUsageAsync();

    Task UpdateSettingsAsync(WorkerUpdateableSettings workerUpdateableSettings);
}