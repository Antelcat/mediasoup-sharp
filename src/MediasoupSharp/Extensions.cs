using MediasoupSharp.ActiveSpeakerObserver;
using MediasoupSharp.Router;

namespace MediasoupSharp;

public static class Extensions
{
    public static async Task<IActiveSpeakerObserver> CreateActiveSpeakerObserverAsync(this IRouter router) =>
       await router.CreateActiveSpeakerObserverAsync<object>();
}