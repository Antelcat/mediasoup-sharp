using Force.DeepCloner;
using Antelcat.LibuvSharp;
using Antelcat.MediasoupSharp;
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
        var defaultWebRtcServerSettings = mediasoupOptions.WebRtcServerOptions;
        var mediasoupServer             = app.ApplicationServices.GetRequiredService<Mediasoup>();
        var numberOfWorkers             = mediasoupOptions.NumWorkers;
        numberOfWorkers = numberOfWorkers is null or <= 0 ? Environment.ProcessorCount : numberOfWorkers;
       
        ThreadPool.QueueUserWorkItem(_ =>
            {
                Loop.Default.Run(() =>
                {
                    for(var c = 0; c < numberOfWorkers; c++)
                    {
                        var worker = app.ApplicationServices.GetRequiredService<WorkerProcess>();
                        worker.On("@success", () =>
                        {
                            //mediasoupServer.AddWorker(worker);
                        });
                    }
                });
            });

        return app;
    }

    private static Task<WebRtcServer> CreateWebRtcServerAsync(Worker worker, ushort portIncrement, WebRtcServerOptions defaultWebRtcServerSettings)
    {
        var webRtcServerSettings = defaultWebRtcServerSettings.DeepClone();
        var listenInfos          = webRtcServerSettings.ListenInfos!;
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