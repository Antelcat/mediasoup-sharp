namespace Antelcat.MediasoupSharp.Demo.Lib;

public class Interactive
{
	private static readonly Mediasoup mediasoup = new();

	private static Worker.Worker?             worker;
	private static WebRtcServer.WebRtcServer? webRtcServer;
	private static Router.Router?             router;
	private static Transport.Transport?       transport;
	private static Producer.Producer?         producer;
	private static Consumer.Consumer?         consumer;
	private static DataProducer.DataProducer? dataProducer;
	private static DataConsumer.DataConsumer? dataConsumer;

	private static readonly Dictionary<int, Worker.Worker>                workers       = [];
	private static readonly Dictionary<string, Router.Router>             routers       = [];
	private static readonly Dictionary<string, WebRtcServer.WebRtcServer> webRtcServers = [];
	private static readonly Dictionary<string, Transport.Transport>       transports    = [];
	private static readonly Dictionary<string, Producer.Producer>         producers     = [];
	private static readonly Dictionary<string, Consumer.Consumer>         consumers     = [];
	private static readonly Dictionary<string, DataProducer.DataProducer> dataProducers = [];
	private static readonly Dictionary<string, DataConsumer.DataConsumer> dataConsumers = [];

	private static void RunMediasoupObserver()
	{
		mediasoup.Observer.On("newworker", (Worker.Worker worker) =>
		{
			// Store the latest worker in a global variable.
			Interactive.worker = worker;

			workers.Add(worker.Pid, worker);
			worker.Observer.On("close", () => workers.Remove(worker.Pid));

			worker.Observer.On("newwebrtcserver", (WebRtcServer.WebRtcServer webRtcServer) =>
			{
				// Store the latest webRtcServer in a global variable.
				Interactive.webRtcServer = webRtcServer;

				webRtcServers.Add(webRtcServer.Id, webRtcServer);
				webRtcServer.Observer.On("close", () => webRtcServers.Remove(webRtcServer.Id));
			});

			worker.Observer.On("newrouter", (Router.Router router) =>
			{
				// Store the latest router in a global variable.
				Interactive.router = router;

				routers.Add(router.Id, router);
				router.Observer.On("close", () => routers.Remove(router.Id));

				router.Observer.On("newtransport", (Transport.Transport transport) =>
				{
					// Store the latest transport in a global variable.
					Interactive.transport = transport;

					transports.Add(transport.Id, transport);
					transport.Observer.On("close", () => transports.Remove(transport.Id));

					transport.Observer.On("newproducer", (Producer.Producer producer) =>
					{
						// Store the latest producer in a global variable.
						Interactive.producer = producer;

						producers.Add(producer.Id, producer);
						producer.Observer.On("close", () => producers.Remove(producer.Id));
					});

					transport.Observer.On("newconsumer", (Consumer.Consumer consumer) =>
					{
						// Store the latest consumer in a global variable.
						Interactive.consumer = consumer;

						consumers.Add(consumer.Id, consumer);
						consumer.Observer.On("close", () => consumers.Remove(consumer.Id));
					});

					transport.Observer.On("newdataproducer", (DataProducer.DataProducer dataProducer) =>
					{
						// Store the latest dataProducer in a global variable.
						Interactive.dataProducer = dataProducer;

						dataProducers.Add(dataProducer.Id, dataProducer);
						dataProducer.Observer.On("close", () => dataProducers.Remove(dataProducer.Id));
					});

					transport.Observer.On("newdataconsumer", (DataConsumer.DataConsumer dataConsumer) =>
					{
						// Store the latest dataConsumer in a global variable.
						Interactive.dataConsumer = dataConsumer;

						dataConsumers.Add(dataConsumer.Id, dataConsumer);
						dataConsumer.Observer.On("close", () => dataConsumers.Remove(dataConsumer.Id));
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