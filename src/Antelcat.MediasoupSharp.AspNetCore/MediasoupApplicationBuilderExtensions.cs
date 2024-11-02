using Antelcat.LibuvSharp;
using Antelcat.MediasoupSharp;
using Antelcat.MediasoupSharp.AspNetCore;
using Antelcat.MediasoupSharp.Internals.Extensions;
using Antelcat.MediasoupSharp.Logger;
using Antelcat.MediasoupSharp.Settings;
using Antelcat.MediasoupSharp.WebRtcServer;
using Antelcat.MediasoupSharp.Worker;

// ReSharper disable once CheckNamespace
namespace Microsoft.AspNetCore.Builder;

public static class MediasoupApplicationBuilderExtensions
{
    public static IApplicationBuilder UseMediasoup(this IApplicationBuilder app)
    {
        var service                     = app.ApplicationServices;
        var loggerFactory               = service.GetService<ILoggerFactory>();
        if (loggerFactory != null) Logger.LoggerFactory = loggerFactory;
        var mediasoupOptions            = app.ApplicationServices.GetRequiredService<MediasoupOptions>();
        if (mediasoupOptions.NumWorkers is null or <= 0) mediasoupOptions.NumWorkers = Environment.ProcessorCount;
        var mediasoupService = app.ApplicationServices.GetRequiredService<MediasoupService>();
        
        return app;
    }

    private static Task<WebRtcServer> CreateWebRtcServerAsync(Worker worker, ushort portIncrement, WebRtcServerOptions defaultWebRtcServerSettings)
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

        var webRtcServerOptions = new WebRtcServerOptions
        {
            ListenInfos = listenInfos,
            AppData     = new Dictionary<string, object>()
        };
        return worker.CreateWebRtcServerAsync(webRtcServerOptions);
    }
}