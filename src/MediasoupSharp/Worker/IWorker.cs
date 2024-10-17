using FBS.Worker;
using MediasoupSharp.EventEmitter;
using MediasoupSharp.Router;
using MediasoupSharp.Settings;

namespace MediasoupSharp.Worker;

public interface IWorker : IEventEmitter, IDisposable
{
    Dictionary<string, object> AppData { get; }

    EventEmitter.EventEmitter Observer { get; }

    Task CloseAsync();

    Task<Router.Router> CreateRouterAsync(RouterOptions routerOptions);

    Task<DumpResponseT> DumpAsync();

    Task<ResourceUsageResponseT> GetResourceUsageAsync();

    Task UpdateSettingsAsync(WorkerUpdateableSettings workerUpdateableSettings);
}