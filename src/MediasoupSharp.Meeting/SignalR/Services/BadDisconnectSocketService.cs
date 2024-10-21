using Microsoft.AspNetCore.SignalR;

namespace MediasoupSharp.Meeting.SignalR.Services
{
    public class BadDisconnectSocketService(ILogger<BadDisconnectSocketService> logger)
    {
        private readonly Dictionary<string, HubCallerContext> cache     = new();
        private readonly object                               cacheLock = new();

        public void DisconnectClient(string connectionId)
        {
            lock(cacheLock)
            {
                if (!cache.TryGetValue(connectionId, out var context)) return;
                context.Abort();
                cache.Remove(connectionId);
            }
        }

        public void CacheContext(HubCallerContext context)
        {
            lock(cacheLock)
            {
                cache[context.ConnectionId] = context;
            }
        }
    }
}
