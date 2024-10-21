using FBS.Worker;
using MediasoupSharp.EnhancedEvent;
using MediasoupSharp.Router;
using MediasoupSharp.Settings;

namespace MediasoupSharp.Worker;

public interface IWorker : IEventEmitter, IDisposable
{
    Dictionary<string, object> AppData { get; }

    EnhancedEvent.EnhancedEventEmitter Observer { get; }

    Task CloseAsync();

    Task<Router.Router> CreateRouterAsync(RouterOptions routerOptions);

    Task<DumpResponseT> DumpAsync();

    Task<ResourceUsageResponseT> GetResourceUsageAsync();

    Task UpdateSettingsAsync(WorkerUpdateableSettings workerUpdateableSettings);
}