using System.Data;
using System.Diagnostics;
using System.Runtime.InteropServices.JavaScript;
using Antelcat.AspNetCore.ProtooSharp;
using Antelcat.MediasoupSharp.ActiveSpeakerObserver;
using Antelcat.MediasoupSharp.ClientRequest;
using Antelcat.MediasoupSharp.Consumer;
using Antelcat.MediasoupSharp.DataConsumer;
using Antelcat.MediasoupSharp.DataProducer;
using Antelcat.MediasoupSharp.Demo.Extensions;
using Antelcat.MediasoupSharp.Exceptions;
using Antelcat.MediasoupSharp.PlainTransport;
using Antelcat.MediasoupSharp.Producer;
using Antelcat.MediasoupSharp.RtpObserver;
using Antelcat.MediasoupSharp.RtpParameters;
using Antelcat.MediasoupSharp.SctpParameters;
using Antelcat.NodeSharp.Events;
using FBS.AudioLevelObserver;
using FBS.Common;
using FBS.Consumer;
using FBS.RtpParameters;
using FBS.SctpParameters;
using FBS.WebRtcTransport;
using Force.DeepCloner;
using TraceNotification = FBS.Transport.TraceNotification;
using TraceNotificationT = FBS.Transport.TraceNotificationT;
using WebRtcTransportOptions = Antelcat.MediasoupSharp.WebRtcTransport.WebRtcTransportOptions;

namespace Antelcat.MediasoupSharp.Demo.Lib;

public class Room : EventEmitter
{
    private readonly ILogger                              logger;
    private readonly string                               roomId;
    private          bool                                 closed;
    private readonly Antelcat.AspNetCore.ProtooSharp.Room protooRoom;

    private class Broadcaster
    {
	    public required string          Id   { get; set; }
	    public required BroadcasterData Data { get; set; }
    }
    internal class BroadcasterData
    {
	    public required string                                        DisplayName     { get; set; }
	    public required object                                        Device          { get; set; }
	    public          RtpCapabilities?                              RtpCapabilities { get; set; }
	    public          Dictionary<string, Transport.Transport>       Transports      { get; set; } = [];
	    public          Dictionary<string, Producer.Producer>         Producers       { get; set; } = [];
	    public          Dictionary<string, Consumer.Consumer>         Consumers       { get; set; } = [];
	    public          Dictionary<string, DataProducer.DataProducer> DataProducers   { get; set; } = [];
	    public          Dictionary<string, DataConsumer.DataConsumer> DataConsumers   { get; set; } = [];
    }

    internal class PeerData : BroadcasterData
    {
	    public bool             Consume          { get; set; }
	    public bool             Joined           { get; set; }
	    public SctpCapabilities SctpCapabilities { get; set; }
    }

    /// <summary>
    /// Map of broadcasters indexed by id. Each Object has:
    /// - {string} id
    /// - {object} data
    ///   - {string} displayName
    ///   - {object} device
    ///   - {RTCRtpCapabilities} rtpCapabilities
    ///   - Dictionary{string, Transport} transports
    ///   - Dictionary{string, Producer} producers
    ///   - Dictionary{string, Consumers} consumers
    ///   - Dictionary{string, DataProducer} dataProducers
    ///   - Dictionary{string, DataConsumer} dataConsumers
    /// </summary>
    private readonly Dictionary<string, Broadcaster> broadcasters = [];
    private readonly WebRtcServer.WebRtcServer                   webRtcServer;
    private readonly Router.Router                               mediasoupRouter;
    private readonly AudioLevelObserver.AudioLevelObserver       audioLevelObserver;
    private readonly ActiveSpeakerObserver.ActiveSpeakerObserver activeSpeakerObserver;
    private readonly Bot                                         bot;
    private readonly int                                         consumerReplicas;
    private          bool                                        networkThrottled;
    
    public static async Task<Room> CreateAsync(
        ILoggerFactory loggerFactory,
        MediasoupOptions options,
        Worker.Worker mediasoupWorker, 
        string roomId, 
        int consumerReplicas)
    {
        // Create a protoo Room instance.
        var protooRoom = new Antelcat.AspNetCore.ProtooSharp.Room(loggerFactory);

        // Router media codecs.
        var  mediaCodecs  = options.MediasoupSettings.RouterSettings.RtpCodecCapabilities;

        // Create a mediasoup Router.
        var mediasoupRouter = await mediasoupWorker.CreateRouterAsync(new()
        {
            MediaCodecs = mediaCodecs
        });

        // Create a mediasoup AudioLevelObserver.
        var audioLevelObserver = await mediasoupRouter.CreateAudioLevelObserverAsync(new()
        {
            MaxEntries = 1,
            Threshold  = -80,
            Interval   = 800
        });

        // Create a mediasoup ActiveSpeakerObserver.
        var activeSpeakerObserver = await mediasoupRouter.CreateActiveSpeakerObserverAsync(new());

        var bot = await Bot.CreateAsync(loggerFactory, mediasoupRouter);

        return new Room(
            loggerFactory.CreateLogger<Room>(),
            roomId,
            protooRoom,
            webRtcServer : mediasoupWorker.AppData["webRtcServer"] as WebRtcServer.WebRtcServer ?? throw new ArgumentNullException(),
            mediasoupRouter,
            audioLevelObserver,
            activeSpeakerObserver,
            consumerReplicas,
            bot
        );
    }

    public Room(ILogger logger,
                string roomId,
                Antelcat.AspNetCore.ProtooSharp.Room protooRoom,
                WebRtcServer.WebRtcServer webRtcServer,
                Router.Router mediasoupRouter,
                AudioLevelObserver.AudioLevelObserver audioLevelObserver,
                ActiveSpeakerObserver.ActiveSpeakerObserver activeSpeakerObserver,
                int consumerReplicas,
                Bot bot)
    {
        MaxListeners               = int.MaxValue;
        this.logger                = logger;
        this.roomId                = roomId;
        this.protooRoom            = protooRoom;
        this.webRtcServer          = webRtcServer;
        this.mediasoupRouter       = mediasoupRouter;
        this.audioLevelObserver    = audioLevelObserver;
        this.activeSpeakerObserver = activeSpeakerObserver;
        this.consumerReplicas      = consumerReplicas;
        this.bot                   = bot;
        
        HandleAudioLevelObserver();
        
        HandleActiveSpeakerObserver();
    }

    public async Task CloseAsync()
    {
        logger.LogDebug($"{nameof(CloseAsync)}()");

        closed = true;
        
        // Close the protoo Room.
        await protooRoom.CloseAsync();
        
        // Close the mediasoup Router.
        await mediasoupRouter.CloseAsync();
        
        // Close the Bot.
        bot.Close(); 
        
        // Close the Bot.
        Emit("close");
        
        // Stop network throttling.
        if (networkThrottled)
        {
            logger.LogDebug($"{nameof(CloseAsync)}() | stopping network throttle");

            //TODO:还不懂
            /*throttle.stop({})
            .catch((error) =>
            {
                logger.error($"close() | failed to stop network throttle:${error}");
            });*/
        }
    }
    
    private void LogStatus()
    {
        logger.LogInformation(
            $"{nameof(LogStatus)}() [{{roomId}}, protoo {{Peers}}]",
            roomId,
            protooRoom.Peers.Count);
    }

    private void HandleProtooConnection(string peerId, bool consume, WebSocketTransport protooWebSocketTransport)
    {
        var existingPeer = protooRoom.GetPeer(peerId);

		if (existingPeer != null)
		{
			logger.LogWarning(
				$"{nameof(HandleProtooConnection)}() | there is already a protoo Peer with same peerId, closing it [{{PeerId}}]",
				peerId);

			_ = existingPeer.CloseAsync();
		}

		Peer peer;

		// Create a new protoo Peer with the given peerId.
		try
		{
			peer = protooRoom.CreatePeer(peerId, protooWebSocketTransport);
		}
		catch (Exception ex)
		{
			logger.LogError($"{nameof(protooRoom)}.{nameof(Antelcat.AspNetCore.ProtooSharp.Room.CreatePeer)}() {{Exception}}", ex);
			return;
		}

		// Use the peer.data object to store mediasoup related objects.

		peer.Data.Set(new PeerData
		{
			// Not joined after a custom protoo 'join' request is later received.
			Consume          = consume,
			Joined           = false,
			DisplayName      = string.Empty,
			Device           = string.Empty,
			RtpCapabilities  = null!,
			SctpCapabilities = null!,

			// Have mediasoup related maps ready even before the Peer joins since we
			// allow creating Transports before joining.
			Transports    = [],
			Producers     = [],
			Consumers     = [],
			DataProducers = [],
			DataConsumers = []
		});
		
		peer.Request += async request =>
		{
			logger.LogDebug(
				"protoo Peer 'request' event [{Method}, {PeerId}]",
				request.Request.Request.Method, peer.Id);

			HandleProtooRequest(peer, request)
				.Catch(exception =>
				{
					logger.LogError("request {Exception}", exception);

					if (exception is ProtooException protooException)
					{
						request.Reject(protooException.ErrorCode, protooException.ErrorReason);
					}
					else
					{
						request.Reject(503, exception?.Message ?? string.Empty);
					}
				});
		};

		peer.Close += async () =>
		{
			if (closed)
				return;

			logger.LogDebug("protoo Peer 'Close' event [{PeerId}]", peer.Id);

			// If the Peer was joined, notify all Peers.
			if (peer.Data.As<PeerData>().Joined)
			{
				foreach (var otherPeer in GetJoinedPeers( peer))
				{
					otherPeer.NotifyAsync("peerClosed", new { peerId = peer.Id })
						.Catch(() => { });
				}
			}

			// Iterate and close all mediasoup Transport associated to this Peer, so all
			// its Producers and Consumers will also be closed.
			foreach (var transport in peer.Data.As<PeerData>()!.Transports.Values)
			{
				await transport.CloseAsync();
			}

			// If this is the latest Peer in the room, close the room.
			if (protooRoom.Peers.Count == 0)
			{
				logger.LogInformation(
					"last Peer in the room left, closing the room [{RoomId}]", roomId);

				await CloseAsync();
			}
		};
    }

    public RtpCapabilities RouterRtpCapabilities => mediasoupRouter.Data.RtpCapabilities;
    
    /**
	 * Create a Broadcaster. This is for HTTP API requests (see server.js).
	 *
	 * @async
	 *
	 * @type {String} id - Broadcaster id.
	 * @type {String} displayName - Descriptive name.
	 * @type {Object} [device] - Additional info with name, version and flags fields.
	 * @type {RTCRtpCapabilities} [rtpCapabilities] - Device RTP capabilities.
	 */
	private async Task<object> CreateBroadcasterAsync(string id,
	                                          string displayName,
	                                          Dictionary<string,object> device, 
	                                          RtpCapabilities? rtpCapabilities)
	{
		if (device["name"] is not string name)
		{
			throw new ArgumentException("missing body.device.name");
		}

		if (broadcasters.ContainsKey(id)) 
			throw new DuplicateNameException($"broadcaster with id {id} already exists");

		var broadcaster = new Broadcaster
		{
			Id = id,
			Data = new BroadcasterData
			{
				DisplayName = displayName,
				Device = new
				{
					flag    = "broadcaster",
					name    = name ?? "Unknown device",
					version = device["version"] as string
				},
				RtpCapabilities = rtpCapabilities,
				Transports    = [],
				Producers     = [],
				Consumers     = [],
				DataProducers = [],
				DataConsumers = [],
			}
		};

		// Store the Broadcaster into the map.
		broadcasters.Add(broadcaster.Id, broadcaster);

		// Notify the new Broadcaster to all Peers.
		foreach (var otherPeer in GetJoinedPeers())
		{
			otherPeer.NotifyAsync(
					"newPeer", new
					{
						id          = broadcaster.Id,
						displayName = broadcaster.Data.DisplayName,
						device      = broadcaster.Data.Device
					})
				.Catch(() => { });
		}

		// Reply with the list of Peers and their Producers.
		var peerInfos   = (List<object>) [];
		var joinedPeers = GetJoinedPeers();

		// Just fill the list of Peers if the Broadcaster provided its rtpCapabilities.
		if (rtpCapabilities != null)
		{
			foreach (var joinedPeer in joinedPeers)
			{
				var peerInfo = new
				{
					id          = joinedPeer.Id,
					displayName = joinedPeer.Data.As<PeerData>().DisplayName,
					device      = joinedPeer.Data.As<PeerData>().Device,
					producers   = new List<object>()
				};

				foreach (var producer in joinedPeer.Data.As<PeerData>().Producers.Values)
				{
					// Ignore Producers that the Broadcaster cannot consume.
					if (!await mediasoupRouter.CanConsumeAsync(producer.Id, rtpCapabilities))
					{
						continue;
					}

					peerInfo.producers.Add(new 
						{
							id   = producer.Id,
							kind = producer.Kind
						});
				}

				peerInfos.Add(peerInfo);
			}
		}

		return peerInfos ;
	}

	public async Task DeleteBroadcasterAsync(string broadcasterId)
	{
		var broadcaster = broadcasters.GetValueOrDefault(broadcasterId);

		if (broadcaster == null)
			throw new KeyNotFoundException($"broadcaster with id {broadcasterId} does not exist");

		foreach (var transport in broadcaster.Data.Transports.Values)
		{
			await transport.CloseAsync();
		}

		broadcasters.Remove(broadcasterId);

		foreach (var peer in GetJoinedPeers())
		{
			peer.NotifyAsync("peerClosed", new { peerId = broadcasterId })
				.Catch(() => { });
		}
	}
	
	public async Task<object> CreateBroadcasterTransportAsync(
		string broadcasterId,
		string type,
		bool rtcpMux = false,
		bool comedia = true,
		SctpCapabilities? sctpCapabilities = null)
	{
		var broadcaster = broadcasters.GetValueOrDefault(broadcasterId);

		if (broadcaster == null)
			throw new KeyNotFoundException($"broadcaster with id {broadcasterId} does not exist");

		switch (type)
		{
			case "webrtc":
			{
				var options = MediasoupOptions.Default.MediasoupSettings.WebRtcTransportSettings;
				var webRtcTransportOptions = new WebRtcTransportOptions
				{
					EnableSctp         = sctpCapabilities is not null,
					NumSctpStreams     = sctpCapabilities?.NumStreams,
					ListenInfos        = options.ListenInfos,
					MaxSctpMessageSize = options.MaxSctpMessageSize ?? 0,
					InitialAvailableOutgoingBitrate = options.InitialAvailableOutgoingBitrate ?? 0,
				};

				var transport = await mediasoupRouter.CreateWebRtcTransportAsync(webRtcTransportOptions with
				{
					WebRtcServer = webRtcServer
				});

				// Store it.
				broadcaster.Data.Transports.Add(transport.Id, transport);

				return new {
					id             = transport.Id,
					iceParameters  = transport.Data.IceParameters,
					iceCandidates  = transport.Data.IceParameters,
					dtlsParameters = transport.Data.DtlsParameters,
					sctpParameters = (object?)null
				};
			}

			case "plain":
			{
				var options = MediasoupOptions.Default.MediasoupSettings.PlainTransportSettings;
				var plainTransportOptions = new PlainTransportOptions
				{
					ListenInfo         = options.ListenInfo!,
					MaxSctpMessageSize = options.MaxSctpMessageSize ?? 0,
					RtcpMux            = rtcpMux,
					Comedia            = comedia
				};

				var transport = await mediasoupRouter.CreatePlainTransportAsync(plainTransportOptions);

				// Store it.
				broadcaster.Data.Transports.Add(transport.Id, transport);

				return new
				{
					id       = transport.Id,
					ip       = transport.Data.Tuple.LocalAddress,
					port     = transport.Data.Tuple.LocalPort,
					rtcpPort = transport.Data.RtcpTuple?.LocalPort
				};
			}

			default:
			{
				throw new ArgumentException("invalid type");
			}
		}
	}
	
	public async Task ConnectBroadcasterTransportAsync(
		string broadcasterId,
		string transportId,
		DtlsParameters dtlsParameters)
	{
		var broadcaster = broadcasters.GetValueOrDefault(broadcasterId);

		if (broadcaster == null)
			throw new KeyNotFoundException($"broadcaster with id {broadcasterId} does not exist");

		var transport = broadcaster.Data.Transports.GetValueOrDefault(transportId);

		if (transport == null)
			throw new KeyNotFoundException($"transport with id {transportId} does not exist");

		if (transport is not WebRtcTransport.WebRtcTransport)
		{
			throw new ArgumentException($"transport with id {transportId} is not a WebRtcTransport");
		}

		await transport.ConnectAsync(dtlsParameters);
	}
	
	public async Task<string> CreateBroadcasterProducerAsync(
		string broadcasterId,
		string transportId,
		MediaKind kind,
		RtpParameters.RtpParameters rtpParameters
	)
	{
		var broadcaster = broadcasters.GetValueOrDefault(broadcasterId);

		if (broadcaster == null)
			throw new KeyNotFoundException($"broadcaster with id {broadcasterId} does not exist");

		var transport = broadcaster.Data.Transports.GetValueOrDefault(transportId);

		if (transport == null)
			throw new KeyNotFoundException($"transport with id {transportId} does not exist");

		var producer =
			await transport.ProduceAsync(new ProducerOptions
			{
				Kind = kind,
				RtpParameters = rtpParameters
			});

		// Store it.
		broadcaster.Data.Producers.Add(producer.Id, producer);

		// Set Producer events.
		// producer.on("score", (score) =>
		// {
		// 	logger.debug(
		// 		"broadcaster producer 'score' event [{producerId}, score:%o]",
		// 		producer.id, score);
		// });

		producer.On("videoorientationchange", async videoOrientation =>
		{
			logger.LogDebug(
				"broadcaster producer 'videoorientationchange' event [{ProducerId}, {VideoOrientation}]",
				producer.Id, videoOrientation[0]);
		});

		// Optimization: Create a server-side Consumer for each Peer.
		foreach (var peer in GetJoinedPeers())
		{
			this._createConsumer(
			{
				consumerPeer : peer,
				producerPeer : broadcaster,
				producer
			});
		}

		// Add into the AudioLevelObserver and ActiveSpeakerObserver.
		if (producer.Kind == MediaKind.AUDIO)
		{
			audioLevelObserver.AddProducerAsync(new RtpObserverAddRemoveProducerOptions
				{
					ProducerId = producer.Id
				})
				.Catch(() => { });

			activeSpeakerObserver.AddProducerAsync(new RtpObserverAddRemoveProducerOptions
				{
					ProducerId = producer.Id
				})
				.Catch(() => { });
		}

		return producer.Id;
	}
	
	public async Task<(
		string id,
		string producerId,
		MediaKind kindind,
		RtpParameters.RtpParameters rtpParameters,
		FBS.RtpParameters.Type type)>  
		CreateBroadcasterConsumerAsync(
		string broadcasterId,
		string transportId,
		string producerId)
	{
		var broadcaster = broadcasters.GetValueOrDefault(broadcasterId);

		if (broadcaster == null)
			throw new KeyNotFoundException($"broadcaster with id {broadcasterId} does not exist");

		if (broadcaster.Data.RtpCapabilities == null)
			throw new NullReferenceException("broadcaster does not have rtpCapabilities");

		var transport = broadcaster.Data.Transports.GetValueOrDefault(transportId);

		if (transport == null)
			throw new KeyNotFoundException($"transport with id {transportId} does not exist");

		var consumer = await transport.ConsumeAsync(new ConsumerOptions
		{
			ProducerId      = producerId,
			RtpCapabilities = broadcaster.Data.RtpCapabilities
		});

		// Store it.
		broadcaster.Data.Consumers.Add(consumer.Id, consumer);

		// Set Consumer events.
		consumer.On("transportclose",async _ =>
		{
			// Remove from its map.
			broadcaster.Data.Consumers.Remove(consumer.Id);
		});

		consumer.On("producerclose", async _ =>
		{
			// Remove from its map.
			broadcaster.Data.Consumers.Remove(consumer.Id);
		});

		return (consumer.Id,
			producerId,
			consumer.Data.Kind,
			consumer.Data.RtpParameters,
			consumer.Data.Type);
	}
	
	public async Task<(string id,ushort? streamId)> CreateBroadcasterDataConsumerAsync(
		string broadcasterId,
		string transportId,
		string dataProducerId)
	{
		var broadcaster = broadcasters.GetValueOrDefault(broadcasterId);

		if (broadcaster == null)
			throw new KeyNotFoundException($"broadcaster with id {broadcasterId} does not exist");

		if (broadcaster.Data.RtpCapabilities == null)
			throw new NullReferenceException("broadcaster does not have rtpCapabilities");

		var transport = broadcaster.Data.Transports.GetValueOrDefault(transportId);

		if (transport == null)
			throw new KeyNotFoundException($"transport with id {transportId} does not exist");

		var dataConsumer = await transport.ConsumeDataAsync(new DataConsumerOptions
		{
			DataProducerId = dataProducerId
		});

		// Store it.
		broadcaster.Data.DataConsumers.Add(dataConsumer.Id, dataConsumer);

		// Set Consumer events.
		dataConsumer.On("transportclose", async _ =>
		{
			// Remove from its map.
			broadcaster.Data.DataConsumers.Remove(dataConsumer.Id);
		});

		dataConsumer.On("dataproducerclose", async _ =>
		{
			// Remove from its map.
			broadcaster.Data.DataConsumers.Remove(dataConsumer.Id);
		});

		return (
			dataConsumer.Id, 
			dataConsumer.Data.SctpStreamParameters?.StreamId
			);
	}
	
	public async Task<string> CreateBroadcasterDataProducerAsync(
		string broadcasterId,
		string transportId,
		string label,
		string protocol,
		SctpStreamParametersT sctpStreamParameters,
		Dictionary<string,object> appData
	)
	{
		var broadcaster = broadcasters.GetValueOrDefault(broadcasterId);

		if (broadcaster == null)
			throw new KeyNotFoundException($"broadcaster with id {broadcasterId} does not exist");

		// if (!broadcaster.Data().sctpCapabilities)
		// 	throw new Error("broadcaster does not have sctpCapabilities");

		var transport = broadcaster.Data.Transports.GetValueOrDefault(transportId);

		if (transport == null)
			throw new KeyNotFoundException($"transport with id {transportId} does not exist");

		var dataProducer = await transport.ProduceDataAsync(new DataProducerOptions
		{
			SctpStreamParameters = sctpStreamParameters,
			Label                = label,
			Protocol             = protocol,
			AppData              = appData
		});

		// Store it.
		broadcaster.Data.DataProducers.Add(dataProducer.Id, dataProducer);

		// Set Consumer events.
		dataProducer.On("transportclose", async _ =>
		{
			// Remove from its map.
			broadcaster.Data.DataProducers.Remove(dataProducer.Id);
		});

		// // Optimization: Create a server-side Consumer for each Peer.
		// for (const peer of this._getJoinedPeers())
		// {
		// 	this._createDataConsumer(
		// 		{
		// 			dataConsumerPeer : peer,
		// 			dataProducerPeer : broadcaster,
		// 			dataProducer: dataProducer
		// 		});
		// }

		return dataProducer.Id;
	}

	private void HandleAudioLevelObserver()
    {
        audioLevelObserver.On("volumes",async volumes =>
        {
            if (volumes is not [Producer.Producer producer, VolumeT volume]) return;

            logger.LogDebug(
                "audioLevelObserver 'volumes' event [{ProducerId}, {Volume}]",
                producer.Id, volume);

            // Notify all Peers.
            foreach (var peer in GetJoinedPeers())
            {
                peer.NotifyAsync(
                    "activeSpeaker",
                    new
                    {
                        peerId = producer.AppData["peerId"],
                        volume
                    }).Catch(() => { });
            }
        });

        audioLevelObserver.On("silence", async _ =>
        {
            logger.LogDebug("audioLevelObserver 'silence' event");

            // Notify all Peers.
            foreach (var peer in GetJoinedPeers())
            {
                peer.NotifyAsync("activeSpeaker", new { peerId = (object)null! })
                    .Catch(() => { });
            }
        });
    }

    private void HandleActiveSpeakerObserver()
    {
        activeSpeakerObserver.On("dominantspeaker", async args =>
        {
            if (args is not [ActiveSpeakerObserverDominantSpeaker dominantSpeaker]) return;

            logger.LogDebug(
                "activeSpeakerObserver 'dominantspeaker' event [{ProducerId}]",
                dominantSpeaker.Producer?.Id);
        });
    }

    private async Task HandleProtooRequestAsync(Peer peer, Peer.RequestHandler handler)
    {
	    var request = handler.Request.Request;

#pragma warning disable VSTHRD200
	    Task Accept<T>(T? data = default)              => handler.AcceptAsync(data);
	    Task Reject(int errorCode, string errorReason) => handler.RejectAsync(errorCode, errorReason);
#pragma warning restore VSTHRD200
	    switch (request.Method)
		{
			case "getRouterRtpCapabilities":
			{
				await Accept(mediasoupRouter.Data.RtpCapabilities);
				break;
			}

			case "join":
			{
				// Ensure the Peer is not already joined.
				if (peer.Data().Joined)
					throw new InvalidOperationException("Peer already joined");

				var (
					displayName,
					device,
					rtpCapabilities,
					sctpCapabilities
					) = handler.Request
					.WithData<(
						string displayName,
						object device,
						RtpCapabilities rtpCapabilities,
						SctpCapabilities
						sctpCapabilities)>().Data;

				// Store client data into the protoo Peer data object.
				peer.Data().Joined          = true;
				peer.Data().DisplayName    = displayName;
				peer.Data().Device         = device;
				peer.Data().RtpCapabilities  = rtpCapabilities;
				peer.Data().SctpCapabilities = sctpCapabilities;

				// Tell the new Peer about already joined Peers.
				// And also create Consumers for existing Producers.

				var joinedPeers = GetJoinedPeers()
					.Select(x => new Broadcaster
					{
						Id = x.Id,
						Data = x.Data()
					})
					.Concat(broadcasters.Values).ToArray();

				// Reply now the request with the list of joined peers (all but the new one).
				var peerInfos = joinedPeers
					.Where(joinedPeer => joinedPeer.Id != peer.Id)
					.Select(x => new
					{
						id          = x.Id,
						displayName = x.Data.DisplayName,
						device      = x.Data.Device
					});

				await Accept(new { peers = peerInfos });

				// Mark the new Peer as joined.
				peer.Data().Joined = true;

				foreach (var joinedPeer in joinedPeers)
				{
					// Create Consumers for existing Producers.
					foreach (var producer in joinedPeer.Data.Producers.Values)
					{
						this._createConsumer(
							{
								consumerPeer : peer,
								producerPeer : joinedPeer,
								producer
							});
					}

					// Create DataConsumers for existing DataProducers.
					foreach (var dataProducer in joinedPeer.Data.DataProducers.Values)
					{
						if (dataProducer.Data.Label == "bot")
							continue;

						this._createDataConsumer(
							{
								dataConsumerPeer : peer,
								dataProducerPeer : joinedPeer,
								dataProducer
							});
					}
				}

				// Create DataConsumers for bot DataProducer.
				this._createDataConsumer(
					{
						dataConsumerPeer : peer,
						dataProducerPeer : null,
						dataProducer     : this._bot.dataProducer
					});

				// Notify the new Peer to all other Peers.
				foreach (var otherPeer in GetJoinedPeers( peer ))
				{
					otherPeer.NotifyAsync(
						"newPeer",new
						{
							id          = peer.Id,
							displayName = peer.Data().DisplayName,
							device      = peer.Data().Device
						})
						.Catch(() => {});
				}

				break;
			}

			case "createWebRtcTransport":
			{
				// NOTE: Don"t require that the Peer is joined here, so the client can
				// initiate mediasoup Transports and be ready when he later joins.

				var (
					forceTcp,
					producing,
					consuming,
					sctpCapabilities
					) = handler.Request
					.WithData<(bool forceTcp, bool producing, bool consuming, SctpCapabilities? sctpCapabilities)>().Data;

				var options = MediasoupOptions.Default.MediasoupSettings.WebRtcTransportSettings;
				var webRtcTransportOptions = new WebRtcTransportOptions
				{
					ListenInfos = options.ListenInfos,
					MaxSctpMessageSize = options.MaxSctpMessageSize ?? 0,
					InitialAvailableOutgoingBitrate = options.InitialAvailableOutgoingBitrate ?? 0,
					
					EnableSctp = sctpCapabilities is not null,
					NumSctpStreams = sctpCapabilities?.NumStreams,
					AppData = new() { { nameof(producing), producing }, { nameof(consuming), consuming } },
				};

				if (forceTcp)
				{
					webRtcTransportOptions.EnableUdp = false;
					webRtcTransportOptions.EnableTcp = true;
				}

				var transport = await mediasoupRouter.CreateWebRtcTransportAsync(webRtcTransportOptions with
				{
					WebRtcServer = webRtcServer
				});

				transport.On("sctpstatechange",async sctpState =>
				{
					logger.LogDebug("WebRtcTransport 'sctpstatechange' event [{SctpState}]", sctpState[0]);
				});

				transport.On("dtlsstatechange", async dtlsState =>
				{
					if (dtlsState is ["failed" or "closed"])
						logger.LogWarning("WebRtcTransport 'dtlsstatechange' event [{DtlsState}]", dtlsState);
				});

				// NOTE: For testing.
				// await transport.enableTraceEvent([ "probation", "bwe" ]);
				await transport.EnableTraceEventAsync([TraceEventType.BWE]);

				transport.On("trace", async args =>
				{
					if (args is not [TraceNotificationT trace]) return;
					logger.LogDebug(
						"transport 'trace' event [{TransportId}, trace.{Type}, {Trace}]", transport.Id, trace.Type, trace);

					if (trace.Type == TraceEventType.BWE && trace.Direction == TraceDirection.DIRECTION_OUT)
					{
						peer.NotifyAsync(
								"downlinkBwe", new
								{
									desiredBitrate          = trace.Info.AsBweTraceInfo().DesiredBitrate,
									effectiveDesiredBitrate = trace.Info.AsBweTraceInfo().EffectiveDesiredBitrate,
									availableBitrate        = trace.Info.AsBweTraceInfo().AvailableBitrate
								})
							.Catch(() => { });
					}
				});

				// Store the WebRtcTransport into the protoo Peer data Object.
				peer.Data().Transports.Add(transport.Id, transport);

				await Accept(new
				{
					id             = transport.Id,
					iceParameters  = transport.Data.IceParameters,
					iceCandidates  = transport.Data.IceCandidates,
					dtlsParameters = transport.Data.DtlsParameters,
					/*sctpParameters = transport.Data.SctpParameters*/
				});

				var maxIncomingBitrate = MediasoupOptions.Default.MediasoupSettings.WebRtcTransportSettings
					.MaximumIncomingBitrate;

				// If set, apply max incoming bitrate limit.
				if (maxIncomingBitrate is not null)
				{
					try
					{
						await transport.SetMaxIncomingBitrateAsync(maxIncomingBitrate.Value);
					}
					catch
					{
						//
					}
				}

				break;
			}

			case "connectWebRtcTransport":
			{
				var (transportId, dtlsParameters) =
					handler.Request.WithData<(string transportId, DtlsParameters dtlsParameters)>()!.Data;
				var transport = peer.Data().Transports.GetValueOrDefault(transportId);

				if (transport == null)
					throw new KeyNotFoundException($"transport with id {transportId} not found");

				await transport.ConnectAsync(dtlsParameters);

				await Accept<object?>();

				break;
			}

			case "restartIce":
			{
				var (transportId, _) = handler.Request.WithData<(string transportId, string @else)>().Data;
				var transport = peer.Data().Transports.GetValueOrDefault(transportId);

				if (transport is not WebRtcTransport.WebRtcTransport webRtcTransport)
					throw new KeyNotFoundException($"transport with id {transportId} not found");

				var iceParameters = await webRtcTransport.RestartIceAsync();

				await Accept(iceParameters);

				break;
			}

			case "produce":
			{
				// Ensure the Peer is joined.
				if (!peer.Data().Joined)
					throw new InvalidStateException("Peer not yet joined");

				var (transportId, kind, rtpParameters, appData) = handler.Request.WithData<(
					string transportId, 
					MediaKind kind, 
					RtpParameters.RtpParameters rtpParameters, 
					Dictionary<string,object> appData)>().Data;
				var transport = peer.Data().Transports.GetValueOrDefault(transportId);

				if (transport == null)
					throw new Exception($"transport with id {transportId} not found");

				// Add peerId into appData to later get the associated Peer during
				// the 'loudest' event of the audioLevelObserver.
				appData.Add("peerId", peer.Id);

				var producer = await transport.ProduceAsync(new()
				{
					Kind          = kind,
					RtpParameters = rtpParameters,
					AppData       = appData
					// keyFrameRequestDelay: 5000
				});

				// Store the Producer into the protoo Peer data Object.
				peer.Data().Producers.Add(producer.Id, producer);

				// Set Producer events.
				producer.On("score", async score =>
				{
					// logger.debug(
					// 	"producer 'score' event [{producerId}, score:%o]",
					// 	producer.id, score);
					peer.NotifyAsync("producerScore", new { producerId = producer.Id, score = score[0] })
						.Catch(() => { });
				});

				producer.On("videoorientationchange", async (videoOrientation) =>
				{
					logger.LogDebug(
						"producer 'videoorientationchange' event [{ProducerId}, {VideoOrientation}]",
						producer.Id, videoOrientation[0]);
				});

				// NOTE: For testing.
				// await producer.enableTraceEvent([ "rtp", "keyframe", "nack", "pli", "fir" ]);
				// await producer.enableTraceEvent([ "pli", "fir" ]);
				// await producer.enableTraceEvent([ "keyframe" ]);

				producer.On("trace", async args =>
				{
					if (args is not [TraceNotification trace]) return;
					logger.LogDebug(
						"producer 'trace' event [{ProducerId}, trace.{Type}, {Trace}]",
						producer.Id, trace.Type, trace);
				});

				await Accept(new { id = producer.Id });

				// Optimization: Create a server-side Consumer for each Peer.
				foreach (var otherPeer in GetJoinedPeers( peer ))
				{
					await CreateConsumerAsync(otherPeer,peer, producer);
				}

				// Add into the AudioLevelObserver and ActiveSpeakerObserver.
				if (producer.Kind == MediaKind.AUDIO)
				{
					audioLevelObserver.AddProducerAsync(new()
						{
							ProducerId = producer.Id
						})
						.Catch(() => { });

					activeSpeakerObserver.AddProducerAsync(new()
						{
							ProducerId = producer.Id
						})
						.Catch(() => { });
				}

				break;
			}

			case "closeProducer":
			{
				// Ensure the Peer is joined.
				if (!peer.Data().Joined)
					throw new InvalidStateException("Peer not yet joined");

				var ( producerId , _) = handler.Request.WithData<(string producerId, string @else)>().Data;
				var producer = peer.Data().Producers.GetValueOrDefault(producerId);

				if (producer == null)
					throw new KeyNotFoundException($"producer with id '{producerId}' not found");

				await producer.CloseAsync();

				// Remove from its map.
				peer.Data().Producers.Remove(producer.Id);

				await Accept<object>();

				break;
			}

			case "pauseProducer":
			{
				// Ensure the Peer is joined.
				if (!peer.Data().Joined)
					throw new InvalidStateException("Peer not yet joined");

				var producerId = handler.Request.WithData<(string producerId,string @else)>().Data.producerId;
				var producer   = peer.Data().Producers.GetValueOrDefault(producerId);

				if (producer == null)
					throw new KeyNotFoundException($"producer with id '{producerId}' not found");

				await producer.PauseAsync();

				await Accept<object>();

				break;
			}

			case "resumeProducer":
			{
				// Ensure the Peer is joined.
				if (!peer.Data().Joined)
					throw new InvalidStateException("Peer not yet joined");

				var producerId = handler.Request.WithData<(string producerId,string @else)>().Data.producerId;
				var producer = peer.Data().Producers.GetValueOrDefault(producerId);

				if (producer == null)
					throw new KeyNotFoundException($"producer with id '{producerId}' not found");

				await producer.ResumeAsync();

				await Accept<object>();

				break;
			}

			case "pauseConsumer":
			{
				// Ensure the Peer is joined.
				if (!peer.Data().Joined)
					throw new InvalidStateException("Peer not yet joined");

				var consumerId = handler.Request.WithData<(string consumerId, string _)>().Data.consumerId;
				var consumer   = peer.Data().Consumers.GetValueOrDefault(consumerId);

				if (consumer == null)
					throw new KeyNotFoundException($"consumer with id '{consumerId}' not found");

				await consumer.PauseAsync();
				
				await Accept<object>();

				break;
			}

			case "resumeConsumer":
			{
				// Ensure the Peer is joined.
				if (!peer.Data().Joined)
					throw new InvalidStateException("Peer not yet joined");

				var consumerId = handler.Request.WithData<(string consumerId, string _)>().Data.consumerId;
				var consumer = peer.Data().Consumers.GetValueOrDefault(consumerId);

				if (consumer == null)
					throw new KeyNotFoundException($"consumer with id '{consumerId}' not found");

				await consumer.ResumeAsync();

				await Accept<object>();

				break;
			}

			case "setConsumerPreferredLayers":
			{
				// Ensure the Peer is joined.
				if (!peer.Data().Joined)
					throw new InvalidStateException("Peer not yet joined");

				var ( consumerId, spatialLayer, temporalLayer ) = handler.Request.WithData<
					(string consumerId,byte spatialLayer,byte temporalLayer)>().Data;
				var consumer = peer.Data().Consumers.GetValueOrDefault(consumerId);

				if (consumer == null)
					throw new KeyNotFoundException($"consumer with id '{consumerId}' not found");

				await consumer.SetPreferredLayersAsync(new()
				{
					PreferredLayers = new()
					{
						SpatialLayer  = spatialLayer,
						TemporalLayer = temporalLayer
					}
				});

				await Accept<object>();

				break;
			}

			case "setConsumerPriority":
			{
				// Ensure the Peer is joined.
				if (!peer.Data().Joined)
					throw new InvalidStateException("Peer not yet joined");

				var (consumerId, priority) = handler.Request.WithData<(string consumerId, SetConsumerPriorityRequest priority)>().Data;
				var consumer = peer.Data().Consumers.GetValueOrDefault(consumerId);

				if (consumer == null)
					throw new KeyNotFoundException($"consumer with id '{consumerId}' not found");

				await consumer.SetPriorityAsync(priority);

				await Accept<object>();

				break;
			}

			case "requestConsumerKeyFrame":
			{
				// Ensure the Peer is joined.
				if (!peer.Data().Joined)
					throw new InvalidStateException("Peer not yet joined");

				var (consumerId, _) = handler.Request.WithData<(string consumerId, string _)>().Data;
				var consumer = peer.Data().Consumers.GetValueOrDefault(consumerId);

				if (consumer == null)
					throw new KeyNotFoundException($"consumer with id '{consumerId}' not found");

				await consumer.RequestKeyFrameAsync();

				await Accept<object>();

				break;
			}

			case "produceData":
			{
				// Ensure the Peer is joined.
				if (!peer.Data().Joined)
					throw new InvalidStateException("Peer not yet joined");

				var (
					transportId,
					sctpStreamParameters,
					label,
					protocol,
					appData
					) = handler.Request.WithData<(
					string transportId,
					SctpStreamParametersT sctpStreamParameters,
					string label,
					string protocol,
					Dictionary<string, object> appData)>().Data;

				var transport = peer.Data().Transports.GetValueOrDefault(transportId);

				if (transport == null)
					throw new KeyNotFoundException($"transport with id '{transportId}' not found");

				var dataProducer = await transport.ProduceDataAsync(new DataProducerOptions()
				{
					SctpStreamParameters = sctpStreamParameters,
					Label                = label,
					Protocol             = protocol,
					AppData              = appData
				});

				// Store the Producer into the protoo Peer data Object.
				peer.Data().DataProducers.Add(dataProducer.Id, dataProducer);

				await Accept(new { id = dataProducer.Id });

				switch (dataProducer.Data.Label)
				{
					case "chat":
					{
						// Create a server-side DataConsumer for each Peer.
						foreach (var otherPeer in GetJoinedPeers(peer))
						{
							this._createDataConsumer(
							{
								dataConsumerPeer :
								otherPeer,
								dataProducerPeer :
								peer,
								dataProducer
							});
						}

						break;
					}

					case "bot":
					{
						// Pass it to the bot.
						await bot.HandlePeerDataProducerAsync(dataProducer.Id, peer);
						break;
					}
				}

				break;
			}

			case "changeDisplayName":
			{
				// Ensure the Peer is joined.
				if (!peer.Data().Joined)
					throw new InvalidStateException("Peer not yet joined");

				var ( displayName ,_) = handler.Request.WithData<(string displayName, string _)>().Data;
				var oldDisplayName = peer.Data().DisplayName;

				// Store the display name into the custom data Object of the protoo
				// Peer.
				peer.Data().DisplayName = displayName;

				// Notify other joined Peers.
				foreach (var otherPeer in GetJoinedPeers(peer))
				{
					otherPeer.NotifyAsync(
							"peerDisplayNameChanged", new
							{
								peerId = peer.Id,
								displayName,
								oldDisplayName
							})
						.Catch(() => { });
				}

				await Accept<object>();

				break;
			}

			case "getTransportStats":
			{
				var ( transportId , _) = handler.Request.WithData<(string transportId, string _)>().Data;
				var transport = peer.Data().Transports.GetValueOrDefault(transportId);

				if (transport == null)
					throw new KeyNotFoundException($"transport with id '{transportId}' not found");

				var stats = await transport.GetStatsAsync();

				await Accept(stats);

				break;
			}

			case "getProducerStats":
			{
				var ( producerId ,_) = handler.Request.WithData<(string producerId, string _)>().Data;;
				var producer = peer.Data().Producers.GetValueOrDefault(producerId);

				if (producer == null)
					throw new KeyNotFoundException($"producer with id 'producerId' not found");

				var stats = await producer.GetStatsAsync();

				await Accept(stats);

				break;
			}

			case "getConsumerStats":
			{
				var ( consumerId ,_) = handler.Request.WithData<(string consumerId, string _)>().Data;
				var consumer = peer.Data().Consumers.GetValueOrDefault(consumerId);

				if (consumer == null)
					throw new KeyNotFoundException($"consumer with id '{consumerId}' not found");

				var stats = await consumer.GetStatsAsync();

				await Accept(stats);

				break;
			}

			case "getDataProducerStats":
			{
				var ( dataProducerId ,_) = handler.Request.WithData<(string dataProducerId, string _)>().Data;
				var dataProducer = peer.Data().DataProducers.GetValueOrDefault(dataProducerId);

				if (dataProducer == null)
					throw new KeyNotFoundException($"dataProducer with id 'dataProducerId' not found");

				var stats = await dataProducer.GetStatsAsync();

				await Accept(stats);

				break;
			}

			case "getDataConsumerStats":
			{
				var ( dataConsumerId ,_) = handler.Request.WithData<(string dataConsumerId, string _)>().Data;
				var dataConsumer = peer.Data().DataConsumers.GetValueOrDefault(dataConsumerId);

				if (dataConsumer == null)
					throw new KeyNotFoundException($"dataConsumer with id 'dataConsumerId' not found");

				var stats = await dataConsumer.GetStatsAsync();

				await Accept(stats);

				break;
			}

			case "applyNetworkThrottle":
			{
				var DefaultUplink     = 1000000;
				var DefaultDownlink   = 1000000;
				var DefaultRtt        = 0;
				var DefaultPacketLoss = 0;

				var (secret, uplink, downlink, rtt, packetLoss) =
					handler.Request.WithData<(bool? secret, int? uplink, int? downlink, int? rtt, int? packetLoss)>().Data;

				if (secret is not true ||
				    (bool.TryParse(Environment.GetEnvironmentVariable("NETWORK_THROTTLE_SECRET"), out var val) &&
				     val != secret))
				{
					await Reject(403, "operation NOT allowed, modda fuckaa");
					return;
				}

				try
				{
					networkThrottled = true;

					await throttle.start(new
					{
						up         = uplink     ?? DefaultUplink,
						down       = downlink   ?? DefaultDownlink,
						rtt        = rtt        ?? DefaultRtt,
						packetLoss = packetLoss ?? DefaultPacketLoss
					});

						logger.LogWarning(
							"network throttle set [{Uplink}, {Downlink}, {Rtt}, {PacketLoss}]",
							uplink     ?? DefaultUplink,
							downlink   ?? DefaultDownlink,
							rtt        ?? DefaultRtt,
							packetLoss ?? DefaultPacketLoss);

					await Accept<object>();
				}
				catch (Exception ex)
				{
					logger.LogError("network throttle apply failed: {Ex}", ex);

					await Reject(500, ex.ToString());
				}

				break;
			}

			case "resetNetworkThrottle":
			{
				var ( secret , _ ) = handler.Request.WithData<(bool? secret, bool _)>().Data;

				if (secret is not true ||
				    (bool.TryParse(Environment.GetEnvironmentVariable("NETWORK_THROTTLE_SECRET"), out var val) &&
				     val != secret))
				{
					await Reject(403, "operation NOT allowed, modda fuckaa");

					return;
				}

				try
				{
					await throttle.stop({});

					logger.LogWarning("network throttle stopped");

					await Accept<object>();
				}
				catch (Exception ex)
				{
					logger.LogError("network throttle stop failed: {Ex}", ex);

					await Reject(500, ex.ToString());
				}

				break;
			}

			default:
			{
				logger.LogError("unknown request.method '{Method}'", request.Method);

				await Reject(500, $"unknown request.method {request.Method}");
				break;
			}
		}
    }

    private IEnumerable<Peer> GetJoinedPeers(Peer? excludePeer = null) =>
	    protooRoom
		    .Peers
		    .Where(x => x.Data().Joined && x != excludePeer);
    
    private async Task CreateConsumerAsync(Peer consumerPeer,Peer producerPeer,Producer.Producer producer )
	{
		// Optimization:
		// - Create the server-side Consumer in paused mode.
		// - Tell its Peer about it and wait for its response.
		// - Upon receipt of the response, resume the server-side Consumer.
		// - If video, this will mean a single key frame requested by the
		//   server-side Consumer (when resuming it).
		// - If audio (or video), it will avoid that RTP packets are received by the
		//   remote endpoint *before* the Consumer is locally created in the endpoint
		//   (and before the local SDP O/A procedure ends). If that happens (RTP
		//   packets are received before the SDP O/A is done) the PeerConnection may
		//   fail to associate the RTP stream.

		// NOTE: Don't create the Consumer if the remote Peer cannot consume it.
		if (consumerPeer.Data().RtpCapabilities == null || !await mediasoupRouter.CanConsumeAsync(producer.Id, consumerPeer.Data().RtpCapabilities!))
		{
			return;
		}

		// Must take the Transport the remote Peer is using for consuming.
		var transport = consumerPeer.Data().Transports.Values.FirstOrDefault(x => x.AppData["consuming"] is true);

		// This should not happen.
		if (transport == null)
		{
			logger.LogWarning($"{nameof(CreateConsumerAsync)}() | Transport for consuming not found");

			return;
		}

		List<Task> tasks = [];

		var consumerCount = 1 + consumerReplicas;

		for (var i=0; i<consumerCount; i++)
		{
			tasks.Add(Task.Run(async () =>
				{
					// Create the Consumer in paused mode.
					Consumer.Consumer? consumer;

					try
					{
						consumer = await transport.ConsumeAsync(new()
						{
							ProducerId      = producer.Id,
							RtpCapabilities = consumerPeer.Data().RtpCapabilities,
							// Enable NACK for OPUS.
							EnableRtx = true,
							Paused    = true
						});
					}
					catch (Exception ex)
					{
						logger.LogWarning($"{nameof(CreateConsumerAsync)}() | transport.consume():{{Ex}}", ex);

						return;
					}

					// Store the Consumer into the protoo consumerPeer data Object.
					consumerPeer.Data().Consumers.Add(consumer.Id, consumer);

					// Set Consumer events.
					consumer.On("transportclose", async _ =>
					{
						// Remove from its map.
						consumerPeer.Data().Consumers.Remove(consumer.Id);
					});

					consumer.On("producerclose", async _ =>
					{
						// Remove from its map.
						consumerPeer.Data().Consumers.Remove(consumer.Id);

						consumerPeer.NotifyAsync("consumerClosed", new { consumerId = consumer.Id })
							.Catch(() => { });
					});

					consumer.On("producerpause", async _ =>
					{
						consumerPeer.NotifyAsync("consumerPaused", new { consumerId = consumer.Id })
							.Catch(() => { });
					});

					consumer.On("producerresume", async _ =>
					{
						consumerPeer.NotifyAsync("consumerResumed", new { consumerId = consumer.Id })
							.Catch(() => { });
					});

					consumer.On("score", async args =>
					{
						var score = args[0];
						// logger.debug(
						//	 'consumer "score" event [consumerId:%s, score:%o]',
						//	 consumer.id, score);

						consumerPeer.NotifyAsync("consumerScore", new { consumerId = consumer.Id, score })
							.Catch(() => { });
					});

					consumer.On("layerschange", async args =>
					{
						var layers = args[0] as ConsumerLayersT;
						consumerPeer.NotifyAsync(
								"consumerLayersChanged", new
								{
									consumerId    = consumer.Id,
									spatialLayer  = layers?.SpatialLayer,
									temporalLayer = layers?.TemporalLayer
								})
							.Catch(() => { });
					});

					// NOTE: For testing.
					// await consumer.enableTraceEvent([ 'rtp', 'keyframe', 'nack', 'pli', 'fir' ]);
					// await consumer.enableTraceEvent([ 'pli', 'fir' ]);
					// await consumer.enableTraceEvent([ 'keyframe' ]);

					consumer.On("trace", async args =>
					{
						var trace = args[0] as TraceNotificationT;
						logger.LogDebug(
							"consumer 'trace' event [producerId:{ProducerId}, trace.type:{Type}, trace:{Trace}]",
							consumer.Id, trace.Type, trace);
					});

					// Send a protoo request to the remote Peer with Consumer parameters.
					try
					{
						await consumerPeer.RequestAsync(
							"newConsumer", new
							{
								peerId         = producerPeer.Id,
								producerId     = producer.Id,
								id             = consumer.Id,
								kind           = consumer.Data.Kind,
								rtpParameters  = consumer.Data.RtpParameters,
								type           = consumer.Data.Type,
								appData        = producer.AppData,
								producerPaused = consumer.ProducerPaused
							});

						// Now that we got the positive response from the remote endpoint, resume
						// the Consumer so the remote endpoint will receive the a first RTP packet
						// of this new stream once its PeerConnection is already ready to process
						// and associate it.
						await consumer.ResumeAsync();

						consumerPeer.NotifyAsync(
								"consumerScore", new
								{
									consumerId = consumer.Id,
									score      = consumer.Score
								})
							.Catch(() => { });
					}
					catch (Exception ex)
					{
						logger.LogWarning("CreateConsumer() | failed:{Ex}", ex);
					}
				})
			);
		}

		try
		{
			await Task.WhenAll(tasks);
		}
		catch (Exception ex)
		{
			logger.LogWarning($"{nameof(CreateConsumerAsync)}() | failed:{{Ex}}", ex);
		}
	}
}

file static class PeerExtensions
{
	public static Room.PeerData Data(this Peer peer) => peer.Data.As<Room.PeerData>();
}