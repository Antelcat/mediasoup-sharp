namespace Antelcat.MediasoupSharp.Demo.Lib;

public class Interactive
{
	private static readonly Mediasoup Mediasoup = new();

	private static IWorker?             worker;
	private static IWebRtcServer? webRtcServer;
	private static IRouter?             router;
	private static ITransport?       transport;
	private static IProducer?         producer;
	private static IConsumer?         consumer;
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
		Mediasoup.Observer.On("newworker", (IWorker worker) =>
		{
			// Store the latest worker in a global variable.
			Interactive.worker = worker;

			Workers.Add(worker.Pid, worker);
			worker.Observer
				.On("close", () => Workers.Remove(worker.Pid))
				.On("newwebrtcserver", (IWebRtcServer webRtcServer) =>
				{
					// Store the latest webRtcServer in a global variable.
					Interactive.webRtcServer = webRtcServer;

					WebRtcServers.Add(webRtcServer.Id, webRtcServer);
					webRtcServer.Observer.On("close", () => WebRtcServers.Remove(webRtcServer.Id));
				})
				.On("newrouter", (IRouter router) =>
				{
					// Store the latest router in a global variable.
					Interactive.router = router;

					Routers.Add(router.Id, router);
					router.Observer
						.On("close", () => Routers.Remove(router.Id))
						.On("newtransport", (ITransport transport) =>
						{
							// Store the latest transport in a global variable.
							Interactive.transport = transport;

							Transports.Add(transport.Id, transport);
							transport.Observer
								.On("close", () => Transports.Remove(transport.Id))
								.On("newproducer", (IProducer producer) =>
								{
									// Store the latest producer in a global variable.
									Interactive.producer = producer;

									Producers.Add(producer.Id, producer);
									producer.Observer.On("close", () => Producers.Remove(producer.Id));
								})
								.On("newconsumer", (IConsumer consumer) =>
								{
									// Store the latest consumer in a global variable.
									Interactive.consumer = consumer;

									Consumers.Add(consumer.Id, consumer);
									consumer.Observer.On("close", () => Consumers.Remove(consumer.Id));
								})
								.On("newdataproducer", (IDataProducer dataProducer) =>
								{
									// Store the latest dataProducer in a global variable.
									Interactive.dataProducer = dataProducer;

									DataProducers.Add(dataProducer.Id, dataProducer);
									dataProducer.Observer.On("close", () => DataProducers.Remove(dataProducer.Id));
								})
								.On("newdataconsumer", (IDataConsumer dataConsumer) =>
								{
									// Store the latest dataConsumer in a global variable.
									Interactive.dataConsumer = dataConsumer;

									DataConsumers.Add(dataConsumer.Id, dataConsumer);
									dataConsumer.Observer.On("close", () => DataConsumers.Remove(dataConsumer.Id));
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