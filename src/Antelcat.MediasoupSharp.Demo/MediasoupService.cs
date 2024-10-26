using Antelcat.MediasoupSharp.WebRtcServer;
using Antelcat.MediasoupSharp.Worker;

namespace Antelcat.MediasoupSharp.Demo;

public class MediasoupService(ILoggerFactory loggerFactory, MediasoupOptions options)
{
    private readonly ILogger       logger           = loggerFactory.CreateLogger<MediasoupService>();
    private readonly List<IWorker> mediasoupWorkers = [];
    
    public async Task RunMediasoupWorkersAsync()
    {
        var numWorkers = options.MediasoupStartupSettings.NumberOfWorkers ?? Environment.ProcessorCount;

        logger.LogInformation("running {Num} mediasoup Workers...", numWorkers);

        var useWebRtcServer = Environment.GetEnvironmentVariable("MEDIASOUP_USE_WEBRTC_SERVER") != "false";
        
        for (var i = 0; i < numWorkers; i++)
        {
            var worker = await Mediasoup.CreateWorkerAsync(loggerFactory, options);

            worker.On("died", async () =>
            {
                logger.LogError("mediasoup Worker died, exiting in 2 seconds... [pid:{Pid}]", worker.Pid);

                await Task.Delay(2000).ContinueWith(_ => Environment.Exit(1));
            });
            
            mediasoupWorkers.Add(worker);

            if (useWebRtcServer)
            {
                // Each mediasoup Worker will run its own WebRtcServer, so those cannot
                // share the same listening ports. Hence we increase the value in config.js
                // for each Worker.
                var webRtcServerOptions = options.MediasoupSettings.WebRtcServerSettings with { };
                var portIncrement       = mediasoupWorkers.Count - 1;

                foreach (var listenInfo in webRtcServerOptions.ListenInfos)
                {
                    listenInfo.Port += (ushort)portIncrement;
                }

                var webRtcServer = await worker.CreateWebRtcServerAsync(new ()
                {
                    ListenInfos = webRtcServerOptions.ListenInfos
                });

                worker.AppData["webRtcServer"] = webRtcServer;
            }
        }
    }
}