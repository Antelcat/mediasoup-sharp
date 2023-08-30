using MediasoupSharp.Router;
using MediasoupSharp.Settings;

namespace MediasoupSharp.Worker
{
    public interface IWorker : IEventEmitter, IDisposable
    {
        Dictionary<string, object> AppData { get; }
        EventEmitter Observer { get; }

        Task CloseAsync();
        Task<Router.Router> CreateRouterAsync(RouterOptions routerOptions);
        Task<string> DumpAsync();
        Task<string> GetResourceUsageAsync();
        Task UpdateSettingsAsync(WorkerUpdateableSettings workerUpdateableSettings);
    }
}
