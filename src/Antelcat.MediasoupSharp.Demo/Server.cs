namespace Antelcat.MediasoupSharp.Demo;

public class Server
{

    public async Task RunMediasoupWorkersAsync(IConfiguration config)
    {
        var numWorkers = config.mediasoup;

        logger.info('running %d mediasoup Workers...', numWorkers);

        for (let i = 0; i < numWorkers; ++i)
        {
            const worker = await mediasoup.createWorker(
            {
                dtlsCertificateFile : config.mediasoup.workerSettings.dtlsCertificateFile,
                dtlsPrivateKeyFile  : config.mediasoup.workerSettings.dtlsPrivateKeyFile,
                logLevel            : config.mediasoup.workerSettings.logLevel,
                logTags             : config.mediasoup.workerSettings.logTags,
                rtcMinPort          : Number(config.mediasoup.workerSettings.rtcMinPort),
                rtcMaxPort          : Number(config.mediasoup.workerSettings.rtcMaxPort)
            });

            worker.on('died', () =>
            {
                logger.error(
                    'mediasoup Worker died, exiting  in 2 seconds... [pid:%d]', worker.pid);

                setTimeout(() => process.exit(1), 2000);
            });

            mediasoupWorkers.push(worker);

            // Create a WebRtcServer in this Worker.
            if (process.env.MEDIASOUP_USE_WEBRTC_SERVER !== 'false')
            {
                // Each mediasoup Worker will run its own WebRtcServer, so those cannot
                // share the same listening ports. Hence we increase the value in config.js
                // for each Worker.
                const webRtcServerOptions = utils.clone(config.mediasoup.webRtcServerOptions);
                const portIncrement = mediasoupWorkers.length - 1;

                for (const listenInfo of webRtcServerOptions.listenInfos)
                {
                    listenInfo.port += portIncrement;
                }

                const webRtcServer = await worker.createWebRtcServer(webRtcServerOptions);

                worker.appData.webRtcServer = webRtcServer;
            }

            // Log worker resource usage every X seconds.
            setInterval(async () =>
            {
                const usage = await worker.getResourceUsage();

                logger.info('mediasoup Worker resource usage [pid:%d]: %o', worker.pid, usage);
            }, 120000);
        }
    }

}