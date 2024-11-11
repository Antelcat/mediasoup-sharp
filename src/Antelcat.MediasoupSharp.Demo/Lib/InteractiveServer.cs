namespace Antelcat.MediasoupSharp.Demo.Lib;

public class Interactive
{
    private static IWorker?       worker;
    private static IWebRtcServer? webRtcServer;
    private static IRouter?       router;
    private static ITransport?    transport;
    private static IProducer?     producer;
    private static IConsumer?     consumer;
    private static IDataProducer? dataProducer;
    private static IDataConsumer? dataConsumer;

    private static readonly Dictionary<int, IWorker>          Workers       = [];
    private static readonly Dictionary<string, IRouter>       Routers       = [];
    private static readonly Dictionary<string, IWebRtcServer> WebRtcServers = [];
    private static readonly Dictionary<string, ITransport>    Transports    = [];
    private static readonly Dictionary<string, IProducer>     Producers     = [];
    private static readonly Dictionary<string, IConsumer>     Consumers     = [];
    private static readonly Dictionary<string, IDataProducer> DataProducers = [];
    private static readonly Dictionary<string, IDataConsumer> DataConsumers = [];

    private static void RunMediasoupObserver()
    {
        Mediasoup.Observer.On(static x => x.NewWorker, worker =>
        {
            // Store the latest worker in a global variable.
            Interactive.worker = worker;

            Workers.Add(worker.Pid, worker);
            worker.Observer
                .On(static x => x.Close, () => Workers.Remove(worker.Pid))
                .On(static x => x.NewWebrtcServer, webRtcServer =>
                {
                    // Store the latest webRtcServer in a global variable.
                    Interactive.webRtcServer = webRtcServer;

                    WebRtcServers.Add(webRtcServer.Id, webRtcServer);
                    webRtcServer.Observer.On(static x => x.Close, () => WebRtcServers.Remove(webRtcServer.Id));
                })
                .On(static x => x.NewRouter, router =>
                {
                    // Store the latest router in a global variable.
                    Interactive.router = router;

                    Routers.Add(router.Id, router);
                    router.Observer
                        .On(static x => x.Close, () => Routers.Remove(router.Id))
                        .On(static x => x.NewTransport, transport =>
                        {
                            // Store the latest transport in a global variable.
                            Interactive.transport = transport;

                            Transports.Add(transport.Id, transport);
                            (((dynamic)transport).Observer as IEnhancedEventEmitter<TransportObserverEvents>)!
                                .On(static x => x.Close, () => Transports.Remove(transport.Id))
                                .On(static x => x.NewProducer, producer =>
                                {
                                    // Store the latest producer in a global variable.
                                    Interactive.producer = producer;

                                    Producers.Add(producer.Id, producer);
                                    producer.Observer.On(static x => x.Close, () => Producers.Remove(producer.Id));
                                })
                                .On(static x => x.NewConsumer, consumer =>
                                {
                                    // Store the latest consumer in a global variable.
                                    Interactive.consumer = consumer;

                                    Consumers.Add(consumer.Id, consumer);
                                    consumer.Observer.On(static x => x.Close, () => Consumers.Remove(consumer.Id));
                                })
                                .On(static x => x.NewDataProducer, dataProducer =>
                                {
                                    // Store the latest dataProducer in a global variable.
                                    Interactive.dataProducer = dataProducer;

                                    DataProducers.Add(dataProducer.Id, dataProducer);
                                    dataProducer.Observer.On(static x => x.Close,
                                        () => DataProducers.Remove(dataProducer.Id));
                                })
                                .On(static x => x.NewDataConsumer, dataConsumer =>
                                {
                                    // Store the latest dataConsumer in a global variable.
                                    Interactive.dataConsumer = dataConsumer;

                                    DataConsumers.Add(dataConsumer.Id, dataConsumer);
                                    dataConsumer.Observer.On(static x => x.Close,
                                        () => DataConsumers.Remove(dataConsumer.Id));
                                });
                        });
                });
        });
    }

    public static void InteractiveServer()
    {
        // Run the mediasoup observer API.
        RunMediasoupObserver();

        // Make maps global so they can be used during the REPL terminal.
        // global.workers       = workers;
        // global.routers       = routers;
        // global.transports    = transports;
        // global.producers     = producers;
        // global.consumers     = consumers;
        // global.dataProducers = dataProducers;
        // global.dataConsumers = dataConsumers;

        /*var server = net.createServer((socket) =>
        {
            const interactive = new Interactive(socket);

            interactive.openCommandConsole();
        });

        await new Promise((resolve) =>
        {
            try { fs.unlinkSync(SOCKET_PATH); }
            catch (error) {}

            server.listen(SOCKET_PATH, resolve);
        });*/
    }
}