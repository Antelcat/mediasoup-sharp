using Force.DeepCloner;
using Antelcat.LibuvSharp;
using Antelcat.MediasoupSharp;
using Antelcat.MediasoupSharp.Settings;
using Antelcat.MediasoupSharp.WebRtcServer;
using Antelcat.MediasoupSharp.Worker;

// ReSharper disable once CheckNamespace
namespace Microsoft.AspNetCore.Builder;

public static class MediasoupApplicationBuilderExtensions
{
    public static IApplicationBuilder UseMediasoup(this IApplicationBuilder app)
    {
        var loggerFactory               = app.ApplicationServices.GetRequiredService<ILoggerFactory>();
        var logger                      = loggerFactory.CreateLogger<Mediasoup>();
        var mediasoupOptions            = app.ApplicationServices.GetRequiredService<MediasoupOptions>();
        var defaultWebRtcServerSettings = mediasoupOptions.WebRtcServerOptions;
        var mediasoupServer             = app.ApplicationServices.GetRequiredService<Mediasoup>();
        var numberOfWorkers             = mediasoupOptions.NumWorkers;
        numberOfWorkers = numberOfWorkers is null or <= 0 ? Environment.ProcessorCount : numberOfWorkers;

        if(false)
        {
            for(var c = 0; c < numberOfWorkers; c++)
            {
                ThreadPool.QueueUserWorkItem(_ =>
                {
                    try
                    {
                        var threadId = Environment.CurrentManagedThreadId;
                        var worker   = app.ApplicationServices.GetRequiredService<WorkerNative>();
                        worker.On("@success", async _ =>
                        {
                            mediasoupServer.AddWorker(worker);
                            logger.LogInformation("Worker[{ThreadId}] create success", threadId);
                            await CreateWebRtcServerAsync(worker, (ushort)c, defaultWebRtcServerSettings);
                        });
                        worker.Run();
                    }
                    catch(Exception ex)
                    {
                        logger.LogError(ex, "Worker create failure");
                    }
                });
            }
        }
        else
        {
            ThreadPool.QueueUserWorkItem(_ =>
            {
                Loop.Default.Run(() =>
                {
                    for(var c = 0; c < numberOfWorkers; c++)
                    {
                        var worker = app.ApplicationServices.GetRequiredService<Worker>();
                        worker.On("@success", () =>
                        {
                            mediasoupServer.AddWorker(worker);
                            logger.LogInformation("Worker[{ProcessId}] create success", worker.Pid);
                        });
                    }
                });
            });
        }

        return app;
    }

    private static Task<WebRtcServer> CreateWebRtcServerAsync(WorkerBase worker, ushort portIncrement, WebRtcServerOptions defaultWebRtcServerSettings)
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