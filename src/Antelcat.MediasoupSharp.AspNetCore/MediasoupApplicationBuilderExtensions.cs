using Antelcat.MediasoupSharp;
using Antelcat.MediasoupSharp.AspNetCore;
using Antelcat.MediasoupSharp.Internals.Extensions;

// ReSharper disable once CheckNamespace
namespace Microsoft.AspNetCore.Builder;

public static class MediasoupApplicationBuilderExtensions
{
    public static IApplicationBuilder UseMediasoup<T>(this IApplicationBuilder app)
    {
        var service                     = app.ApplicationServices;
        var loggerFactory               = service.GetService<ILoggerFactory>();
        if (loggerFactory != null) Logger.LoggerFactory = loggerFactory;
        
        return app;
    }

    private static Task<WebRtcServer<T>> CreateWebRtcServerAsync<T>(Worker<T> worker, ushort portIncrement, WebRtcServerOptions<T> defaultWebRtcServerSettings)
    where T : new()
    {
        var webRtcServerSettings = defaultWebRtcServerSettings.DeepClone();
        var listenInfos          = webRtcServerSettings.ListenInfos;
        foreach(var listenInfo in listenInfos)
        {
            if(listenInfo.Port != 0)
            {
                listenInfo.Port = (ushort)(listenInfo.Port + portIncrement);
            }
        }

        var webRtcServerOptions = new WebRtcServerOptions<T>
        {
            ListenInfos = listenInfos,
            AppData     = new()
        };
        return worker.CreateWebRtcServerAsync(webRtcServerOptions);
    }
}