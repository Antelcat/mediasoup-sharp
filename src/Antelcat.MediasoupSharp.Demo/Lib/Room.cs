﻿global using TWorkerAppData = System.Collections.Generic.Dictionary<string, object>;
using System.Data;
using Antelcat.AspNetCore.ProtooSharp;
using Antelcat.MediasoupSharp.AspNetCore;
using Antelcat.MediasoupSharp.Demo.Extensions;
using Antelcat.MediasoupSharp.Internals.Extensions;
using Antelcat.NodeSharp.Events;
using Antelcat.MediasoupSharp.FBS.Common;
using Antelcat.MediasoupSharp.FBS.Consumer;
using Antelcat.MediasoupSharp.FBS.RtpParameters;
using Antelcat.MediasoupSharp.FBS.SctpParameters;
using Antelcat.MediasoupSharp.FBS.Transport;
using Antelcat.MediasoupSharp.FBS.WebRtcTransport;
using TraceEventType = Antelcat.MediasoupSharp.FBS.Transport.TraceEventType;

namespace Antelcat.MediasoupSharp.Demo.Lib;

public class Room : EventEmitter
{
    private          bool                                 closed;
    private readonly ILogger                              logger;
    private readonly string                               roomId;
    private readonly Antelcat.AspNetCore.ProtooSharp.Room protooRoom;
    private readonly MediasoupOptions<TWorkerAppData>     options;


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

    private readonly IWebRtcServer                         webRtcServer;
    private readonly RouterImpl<TWorkerAppData>                mediasoupRouter;
    private readonly AudioLevelObserver<TWorkerAppData>    audioLevelObserver;
    private readonly ActiveSpeakerObserverImpl<TWorkerAppData> activeSpeakerObserver;
    private readonly Bot<TWorkerAppData>                   bot;
    private readonly int                                   consumerReplicas;
    private          bool                                  networkThrottled;

    public static async Task<Room> CreateAsync(
        ILoggerFactory loggerFactory,
        MediasoupOptions<TWorkerAppData> options,
        WorkerImpl<TWorkerAppData> mediasoupWorkerProcess,
        string roomId,
        int consumerReplicas)
    {
        // Create a protoo Room instance.
        var protooRoom = new Antelcat.AspNetCore.ProtooSharp.Room(loggerFactory);

        // Router media codecs.
        var mediaCodecs = options.RouterOptions.NotNull().MediaCodecs;

        // Create a mediasoup Router.
        var mediasoupRouter = await mediasoupWorkerProcess.CreateRouterAsync<TWorkerAppData>(new()
        {
            MediaCodecs = mediaCodecs
        });

        // Create a mediasoup AudioLevelObserver.
        var audioLevelObserver = await mediasoupRouter.CreateAudioLevelObserverAsync<TWorkerAppData>(new()
        {
            MaxEntries = 1,
            Threshold  = -80,
            Interval   = 800
        });

        // Create a mediasoup ActiveSpeakerObserver.
        var activeSpeakerObserver = await mediasoupRouter.CreateActiveSpeakerObserverAsync<TWorkerAppData>(new());

        var bot = await Bot<TWorkerAppData>.CreateAsync(loggerFactory, mediasoupRouter);

        return new Room(
            loggerFactory.CreateLogger<Room>(),
            roomId,
            protooRoom,
            options,
            webRtcServer: mediasoupWorkerProcess.AppData[nameof(webRtcServer)] as WebRtcServerImpl<TWorkerAppData> 
                          ?? throw new ArgumentNullException(),
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
                MediasoupOptions<TWorkerAppData> options,
                WebRtcServerImpl<TWorkerAppData> webRtcServer,
                RouterImpl<TWorkerAppData> mediasoupRouter,
                AudioLevelObserver<TWorkerAppData> audioLevelObserver,
                ActiveSpeakerObserverImpl<TWorkerAppData> activeSpeakerObserver,
                int consumerReplicas,
                Bot<TWorkerAppData> bot)
    {
        MaxListeners               = int.MaxValue;
        this.options               = options;
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

            //TODO: watt is that
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
            $"{nameof(LogStatus)}() [roomId:{{RoomId}}, protoo:{{Peers}}]",
            roomId,
            protooRoom.Peers.Count);
    }

    public void HandleProtooConnection(string peerId, bool consume, WebSocketTransport protooWebSocketTransport)
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
            logger.LogError(
                $"{nameof(protooRoom)}.{nameof(Antelcat.AspNetCore.ProtooSharp.Room.CreatePeer)}() Exception:{{Exception}}",
                ex);
            return;
        }

        // Notify mediasoup version to the peer.
        peer.NotifyAsync("mediasoup-version", new { version = Mediasoup.Version.ToString() })
            .Catch(() => { });

        // Use the peer.data object to store mediasoup related objects.

        peer.Data.Set(new PeerData
        {
            // Not joined after a custom protoo 'join' request is later received.
            Consume          = consume,
            Joined           = false,
            DisplayName      = string.Empty,
            Device           = null!,
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
                "protoo Peer 'request' event [method:{Method}, peerId:{PeerId}]",
                request.Request.Request.Method, peer.Id);

            HandleProtooRequestAsync(peer, request)
                .Catch(exception =>
                {
                    logger.LogError("request exception:{Exception}", exception);

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

            logger.LogDebug("protoo Peer 'Close' event [peerId:{PeerId}]", peer.Id);

            // If the Peer was joined, notify all Peers.
            if (peer.Data.As<PeerData>().Joined)
            {
                foreach (var otherPeer in GetJoinedPeers(peer))
                {
                    otherPeer.NotifyAsync("peerClosed", new { peerId = peer.Id })
                        .Catch(() => { });
                }
            }

            // Iterate and close all mediasoup Transport associated to this Peer, so all
            // its Producers and Consumers will also be closed.
            foreach (var transport in peer.Data.As<PeerData>().NotNull().Transports.Values)
            {
                await transport.CloseAsync();
            }

            // If this is the latest Peer in the room, close the room.
            if (protooRoom.Peers.Count == 0)
            {
                logger.LogInformation(
                    "last Peer in the room left, closing the room [roomId:{RoomId}]", roomId);

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
    internal async Task<PeerInfosR> CreateBroadcasterAsync(CreateBroadcasterRequest request)
    {
        var (id, displayName, device, rtpCapabilities) = request;
        if (device.Name is not string name)
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
                Device = new DeviceR
                {
                    Flag    = "broadcaster",
                    Name    = name,
                    Version = device.Version
                },
                RtpCapabilities = rtpCapabilities,
                Transports      = [],
                Producers       = [],
                Consumers       = [],
                DataProducers   = [],
                DataConsumers   = []
            }
        };

        // Store the Broadcaster into the map.
        broadcasters.Add(broadcaster.Id, broadcaster);

        // Notify the new Broadcaster to all Peers.
        foreach (var otherPeer in GetJoinedPeers())
        {
            otherPeer.NotifyAsync(
                    "newPeer", new PeerInfoR
                    {
                        Id          = broadcaster.Id,
                        DisplayName = broadcaster.Data.DisplayName,
                        Device      = broadcaster.Data.Device
                    })
                .Catch(() => { });
        }

        // Reply with the list of Peers and their Producers.
        var peerInfos   = (List<PeerInfoR>) [];
        var joinedPeers = GetJoinedPeers();

        // Just fill the list of Peers if the Broadcaster provided its rtpCapabilities.
        if (rtpCapabilities != null)
        {
            foreach (var joinedPeer in joinedPeers)
            {
                var peerInfo = new PeerInfoR
                {
                    Id          = joinedPeer.Id,
                    DisplayName = joinedPeer.Data.As<PeerData>().DisplayName,
                    Device      = joinedPeer.Data.As<PeerData>().Device,
                    Producers   = (List<PeerProducerR>) []
                };

                foreach (var producer in joinedPeer.Data.As<PeerData>().Producers.Values)
                {
                    // Ignore Producers that the Broadcaster cannot consume.
                    if (!await mediasoupRouter.CanConsumeAsync(producer.Id, rtpCapabilities))
                    {
                        continue;
                    }

                    peerInfo.Producers.Add(new PeerProducerR
                    {
                        Id   = producer.Id,
                        Kind = producer.Kind
                    });
                }

                peerInfos.Add(peerInfo);
            }
        }

        return new PeerInfosR
        {
            Peers = peerInfos
        };
    }

    public async Task DeleteBroadcasterAsync(string broadcasterId)
    {
        var broadcaster = broadcasters.GetValueOrDefault(broadcasterId);

        if (broadcaster == null)
            throw new KeyNotFoundException($"broadcaster with id {broadcasterId} does not exist");

        await broadcaster.Data.Transports.Values.Select(x => x.CloseAsync());

        broadcasters.Remove(broadcasterId);

        foreach (var peer in GetJoinedPeers())
        {
            peer.NotifyAsync("peerClosed", new { peerId = broadcasterId })
                .Catch(() => { });
        }
    }

    public async Task<TransportDataR> CreateBroadcasterTransportAsync(CreateBroadcastTransport request)
    {
        var (broadcasterId, type, rtcpMux, comedia, sctpCapabilities) = request;

        var broadcaster = broadcasters.GetValueOrDefault(broadcasterId);

        if (broadcaster == null)
            throw new KeyNotFoundException($"broadcaster with id {broadcasterId} does not exist");

        switch (type)
        {
            case "webrtc":
            {
                var ops = options.WebRtcTransportOptions.NotNull();
                var webRtcTransportOptions = new WebRtcTransportOptions<TWorkerAppData>
                {
                    ListenInfos                     = ops.ListenInfos,
                    MaxSctpMessageSize              = ops.MaxSctpMessageSize,
                    InitialAvailableOutgoingBitrate = ops.InitialAvailableOutgoingBitrate,

                    WebRtcServer      = webRtcServer,
                    IceConsentTimeout = 20,
                    EnableSctp        = sctpCapabilities is not null,
                    NumSctpStreams    = sctpCapabilities?.NumStreams
                };

                var transport = await mediasoupRouter.CreateWebRtcTransportAsync(webRtcTransportOptions);

                // Store it.
                broadcaster.Data.Transports.Add(transport.Id, transport);

                return new TransportDataR
                {
                    Id             = transport.Id,
                    IceParameters  = transport.Data.IceParameters,
                    IceCandidates  = transport.Data.IceParameters,
                    DtlsParameters = transport.Data.DtlsParameters,
                    SctpParameters = transport.Data.SctpParameters
                };
            }

            case "plain":
            {
                var op = options.PlainTransportOptions.NotNull();
                var plainTransportOptions = new PlainTransportOptions<TWorkerAppData>
                {
                    ListenInfo         = op.ListenInfo.NotNull(),
                    MaxSctpMessageSize = op.MaxSctpMessageSize,
                    RtcpMux            = rtcpMux,
                    Comedia            = comedia
                };

                var transport = await mediasoupRouter.CreatePlainTransportAsync(plainTransportOptions);

                // Store it.
                broadcaster.Data.Transports.Add(transport.Id, transport);

                return new TransportDataR
                {
                    Id       = transport.Id,
                    Ip       = transport.Data.Tuple.LocalAddress,
                    Port     = transport.Data.Tuple.LocalPort,
                    RtcpPort = (ushort?)transport.Data.RtcpTuple?.LocalPort
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

        if (transport is not IWebRtcTransport)
        {
            throw new ArgumentException($"transport with id {transportId} is not a WebRtcTransport");
        }

        await transport.ConnectAsync(dtlsParameters);
    }

    internal async Task<IdR> CreateBroadcasterProducerAsync(
        string broadcasterId,
        string transportId,
        MediaKind kind,
        RtpParameters rtpParameters
    )
    {
        var broadcaster = broadcasters.GetValueOrDefault(broadcasterId);

        if (broadcaster == null)
            throw new KeyNotFoundException($"broadcaster with id {broadcasterId} does not exist");

        var transport = broadcaster.Data.Transports.GetValueOrDefault(transportId);

        if (transport == null)
            throw new KeyNotFoundException($"transport with id {transportId} does not exist");

        var producer =
            await transport.ProduceAsync(new ProducerOptions<TWorkerAppData>
            {
                Kind          = kind,
                RtpParameters = rtpParameters
            });

        // Store it.
        broadcaster.Data.Producers.Add(producer.Id, producer);

        // Set Producer events.
        // producer.On(static x=>x.score, (score) =>
        // {
        // 	logger.debug(
        // 		"broadcaster producer 'score' event [{producerId}, score:%o]",
        // 		producer.id, score);
        // });

        producer.On(static x => x.VideoOrientationChange, videoOrientation =>
        {
            logger.LogDebug(
                "broadcaster producer 'VideoOrientationChange' event [producerId:{ProducerId}, videoOrientation:{VideoOrientation}]",
                producer.Id, videoOrientation);
        });

        // Optimization: Create a server-side Consumer for each Peer.
        foreach (var peer in GetJoinedPeers())
        {
            _ = CreateConsumerAsync(peer, broadcaster.Id, producer);
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

        return new IdR
        {
            Id = producer.Id
        };
    }

    internal async Task<CreateBroadcastConsumerResponseR>
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

        var consumer = await transport.ConsumeAsync(new ConsumerOptions<TWorkerAppData>
        {
            ProducerId      = producerId,
            RtpCapabilities = broadcaster.Data.RtpCapabilities
        });

        // Store it.
        broadcaster.Data.Consumers.Add(consumer.Id, consumer);

        // Set Consumer events.
        consumer.On(static x => x.TransportClose, () =>
        {
            // Remove from its map.
            broadcaster.Data.Consumers.Remove(consumer.Id);
        });

        consumer.On(static x => x.ProducerClose, () =>
        {
            // Remove from its map.
            broadcaster.Data.Consumers.Remove(consumer.Id);
        });

        return new CreateBroadcastConsumerResponseR
        {
            Id            = consumer.Id,
            ProducerId    = producerId,
            Kind          = consumer.Data.Kind,
            RtpParameters = consumer.Data.RtpParameters,
            Type          = consumer.Data.Type
        };
    }

    internal async Task<IdAndStreamIdR> CreateBroadcasterDataConsumerAsync(
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

        var dataConsumer = await transport.ConsumeDataAsync(new DataConsumerOptions<TWorkerAppData>
        {
            DataProducerId = dataProducerId
        });

        // Store it.
        broadcaster.Data.DataConsumers.Add(dataConsumer.Id, dataConsumer);

        // Set Consumer events.
        dataConsumer.On(static x => x.TransportClose, () =>
        {
            // Remove from its map.
            broadcaster.Data.DataConsumers.Remove(dataConsumer.Id);
        });

        dataConsumer.On(static x => x.DataProducerClose, () =>
        {
            // Remove from its map.
            broadcaster.Data.DataConsumers.Remove(dataConsumer.Id);
        });

        return new IdAndStreamIdR
        {
            Id       = dataConsumer.Id,
            StreamId = dataConsumer.Data.SctpStreamParameters.NotNull().StreamId
        };
    }

    public async Task<string> CreateBroadcasterDataProducerAsync(
        string broadcasterId,
        string transportId,
        string label,
        string protocol,
        SctpStreamParametersT sctpStreamParameters,
        TWorkerAppData appData
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

        var dataProducer = await transport.ProduceDataAsync(new DataProducerOptions<TWorkerAppData>
        {
            SctpStreamParameters = sctpStreamParameters,
            Label                = label,
            Protocol             = protocol,
            AppData              = appData
        });

        // Store it.
        broadcaster.Data.DataProducers.Add(dataProducer.Id, dataProducer);

        // Set Consumer events.
        dataProducer.On(static x => x.TransportClose, () =>
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
        audioLevelObserver.On(static x => x.Volumes, volumes =>
        {
            var volume = volumes[0];
            logger.LogDebug("audioLevelObserver 'volumes' event [producerId:{ProducerId}, volume:{Volume}]",
                volume.Producer.Id, volume.Volume);

            // Notify all Peers.
            foreach (var peer in GetJoinedPeers())
            {
                peer.NotifyAsync(
                    "activeSpeaker",
                    new
                    {
                        peerId = volume.Producer.Id,
                        volume.Volume
                    }).Catch(() => { });
            }
        });

        audioLevelObserver.On(static x => x.Silence, () =>
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
        activeSpeakerObserver.On(static x => x.DominantSpeaker, dominantSpeaker =>
        {
            logger.LogDebug(
                "activeSpeakerObserver 'dominantspeaker' event [producerId:{ProducerId}]",
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
                    ) = handler.Request.WithData<JoinRequest>().NotNull().Data.NotNull();

                // Store client data into the protoo Peer data object.
                peer.Data().Joined           = true;
                peer.Data().DisplayName      = displayName;
                peer.Data().Device           = device;
                peer.Data().RtpCapabilities  = rtpCapabilities;
                peer.Data().SctpCapabilities = sctpCapabilities;

                // Tell the new Peer about already joined Peers.
                // And also create Consumers for existing Producers.

                var joinedPeers = GetJoinedPeers()
                    .Select(x => new Broadcaster
                    {
                        Id   = x.Id,
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

                List<Task> tasks = [];

                foreach (var joinedPeer in joinedPeers)
                {
                    // Create Consumers for existing Producers.
                    foreach (var producer in joinedPeer.Data.Producers.Values.ToArray())
                    {
                        CreateConsumerAsync(
                            peer, joinedPeer.Id, producer
                        ).AddTo(tasks);
                    }

                    // Create DataConsumers for existing DataProducers.
                    foreach (var dataProducer in joinedPeer.Data.DataProducers.Values)
                    {
                        if (dataProducer.Data.Label == nameof(bot))
                            continue;

                        CreateDataConsumerAsync(
                            peer,
                            joinedPeer.Id,
                            dataProducer
                        ).AddTo(tasks);
                    }
                }

                // Create DataConsumers for bot DataProducer.
                CreateDataConsumerAsync(peer, null, bot.DataProducer).AddTo(tasks);

                // Notify the new Peer to all other Peers.
                foreach (var otherPeer in GetJoinedPeers(peer))
                {
                    otherPeer.NotifyAsync(
                            "newPeer", new
                            {
                                id          = peer.Id,
                                displayName = peer.Data().DisplayName,
                                device      = peer.Data().Device
                            })
                        .Catch(() => { });
                }

                await tasks;
                break;
            }

            case "createWebRtcTransport":
            {
                // NOTE: Don't require that the Peer is joined here, so the client can
                // initiate mediasoup Transports and be ready when he later joins.

                var (
                    forceTcp,
                    producing,
                    consuming,
                    sctpCapabilities
                    ) = handler.Request
                    .WithData<CreateWebRtcTransportRequest>().NotNull().Data.NotNull();

                var ops = options.WebRtcTransportOptions.NotNull();
                var webRtcTransportOptions = new WebRtcTransportOptions<TWorkerAppData>
                {
                    ListenInfos                     = ops.ListenInfos,
                    MaxSctpMessageSize              = ops.MaxSctpMessageSize,
                    InitialAvailableOutgoingBitrate = ops.InitialAvailableOutgoingBitrate,

                    WebRtcServer      = webRtcServer,
                    IceConsentTimeout = 20,
                    EnableSctp        = sctpCapabilities is not null,
                    NumSctpStreams    = sctpCapabilities?.NumStreams,
                    AppData = new()
                    {
                        { nameof(producing), producing },
                        { nameof(consuming), consuming }
                    }
                };

                if (forceTcp)
                {
                    webRtcTransportOptions.ListenInfos = webRtcTransportOptions
                        .ListenInfos
                        .Where(x => x.Protocol == Protocol.TCP)
                        .ToArray();

                    webRtcTransportOptions.EnableUdp = false;
                    webRtcTransportOptions.EnableTcp = true;
                }

                var transport = await mediasoupRouter.CreateWebRtcTransportAsync(webRtcTransportOptions);

                transport.On(static x => x.IceStateChange, async iceState =>
                {
                    if (iceState == IceState.DISCONNECTED /*|| iceState == IceState.CLOSED*/)
                    {
                        logger.LogWarning($"WebRtcTransport 'IceStateChange' event [{nameof(iceState)}:{{State}}]",
                            iceState);
                        await peer.CloseAsync();
                    }
                });

                transport.On(static x => x.SctpStateChange,
                    sctpState =>
                    {
                        logger.LogDebug($"WebRtcTransport 'SctpStateChange' event [{nameof(sctpState)}:{{SctpState}}]",
                            sctpState);
                    });

                transport.On(static x => x.DtlsStateChange, async dtlsState =>
                {
                    if (dtlsState is DtlsState.FAILED or DtlsState.CLOSED)
                    {
                        logger.LogWarning(
                            $"WebRtcTransport 'DtlsStateChange' event [{nameof(dtlsState)}:{{DtlsState}}]", dtlsState);
                        await peer.CloseAsync();
                    }
                });

                // NOTE: For testing.
                // await transport.enableTraceEvent([ "probation", "bwe" ]);
                await transport.EnableTraceEventAsync([TraceEventType.BWE]);

                transport.On(static x => x.Trace, trace =>
                {
                    logger.LogDebug(
                        "transport 'trace' event [transportId:{TransportId}, trace.type:{Type}, trace:{Trace}]",
                        transport.Id, trace.Type,
                        trace);

                    if (trace is { Type: TraceEventType.BWE, Direction: TraceDirection.DIRECTION_OUT })
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
                    sctpParameters = transport.Data.SctpParameters
                });


                var maxIncomingBitrate = (options.WebRtcTransportOptions
                    as AdditionalWebRtcTransportOptions<TWorkerAppData>)
                    .NotNull()
                    .MaxIncomingBitrate;

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
                    handler.Request.WithData<ConnectWebRtcTransportRequest>().NotNull().Data!;

                var transport = peer.Data().Transports.GetValueOrDefault(transportId);

                if (transport == null)
                    throw new KeyNotFoundException($"transport with id {transportId} not found");

                await transport.ConnectAsync(new ConnectRequestT
                {
                    DtlsParameters = dtlsParameters
                });

                await Accept<object?>();

                break;
            }

            case "restartIce":
            {
                var transportId = handler.Request.WithData<RestartIceRequest>().NotNull().Data.NotNull().TransportId;
                var transport   = peer.Data().Transports.GetValueOrDefault(transportId);

                if (transport is not IWebRtcTransport webRtcTransport)
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

                var (transportId, kind, rtpParameters, appData) =
                    handler.Request.WithData<ProduceRequest<TWorkerAppData>>().NotNull().Data!;

                var transport = peer.Data().Transports.GetValueOrDefault(transportId);

                if (transport == null)
                    throw new Exception($"transport with id {transportId} not found");

                // Add peerId into appData to later get the associated Peer during
                // the 'loudest' event of the audioLevelObserver.
                appData.Add("peerId", peer.Id);

                var producer = await transport.ProduceAsync<TWorkerAppData>(new()
                {
                    Kind          = kind,
                    RtpParameters = rtpParameters,
                    AppData       = appData
                    // keyFrameRequestDelay: 5000
                });

                // Store the Producer into the protoo Peer data Object.
                peer.Data().Producers.Add(producer.Id, producer);

                // Set Producer events.
                producer.On(static x => x.Score, score =>
                {
                    // logger.debug(
                    // 	"producer 'score' event [{producerId}, score:%o]",
                    // 	producer.id, score);
                    peer.NotifyAsync("producerScore", new { producerId = producer.Id, score })
                        .Catch(() => { });
                });

                producer.On(static x => x.VideoOrientationChange, videoOrientation =>
                {
                    logger.LogDebug(
                        "producer 'VideoOrientationChange' event [producerId:{ProducerId}, videoOrientation:{VideoOrientation}]",
                        producer.Id, videoOrientation);
                });

                // NOTE: For testing.
                // await producer.enableTraceEvent([ "rtp", "keyframe", "nack", "pli", "fir" ]);
                // await producer.enableTraceEvent([ "pli", "fir" ]);
                // await producer.enableTraceEvent([ "keyframe" ]);

                producer.On(static x => x.Trace, trace =>
                {
                    logger.LogDebug(
                        "producer 'trace' event [producerId:{ProducerId}, trace.type:{Type}, trace:{Trace}]",
                        producer.Id, trace.Type, trace);
                });

                await Accept(new { id = producer.Id });

                // Optimization: Create a server-side Consumer for each Peer.
                foreach (var otherPeer in GetJoinedPeers(peer))
                {
                    await CreateConsumerAsync(otherPeer, peer.Id, producer);
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

                var producerId = handler.Request.WithData<ProducerRequest>().NotNull().Data.NotNull().ProducerId;
                var producer   = peer.Data().Producers.GetValueOrDefault(producerId);

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

                var producerId = handler.Request.WithData<ProducerRequest>().NotNull().Data.NotNull().ProducerId;

                var producer = peer.Data().Producers.GetValueOrDefault(producerId);

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

                var producerId = handler.Request.WithData<ProducerRequest>().NotNull().Data.NotNull().ProducerId;

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

                var consumerId = handler.Request.WithData<ConsumerRequest>().NotNull().Data.NotNull().ConsumerId;
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

                var consumerId = handler.Request.WithData<ConsumerRequest>().NotNull().Data.NotNull().ConsumerId;
                var consumer   = peer.Data().Consumers.GetValueOrDefault(consumerId);

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

                var (consumerId, spatialLayer, temporalLayer) =
                    handler.Request.WithData<SetConsumerPreferredLayersRequest>().NotNull().Data!;

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

                var (consumerId, priority) = handler.Request.WithData<SetConsumerPriorityRequest>().NotNull().Data!;
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

                var consumerId = handler.Request.WithData<ConsumerRequest>().NotNull().Data.NotNull().ConsumerId;

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
                    ) = handler.Request.WithData<ProduceDataRequest>().NotNull().Data.NotNull();

                var transport = transportId is null ? null : peer.Data().Transports.GetValueOrDefault(transportId);

                if (transport == null)
                    throw new KeyNotFoundException($"transport with id '{transportId}' not found");

                var dataProducer = await transport.ProduceDataAsync<TWorkerAppData>(new()
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
                            await CreateDataConsumerAsync(otherPeer, peer.Id, dataProducer);
                        }

                        break;
                    }

                    case nameof(bot):
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

                var displayName    = handler.Request.WithData<ChangeDisplayNameRequest>().NotNull().Data.NotNull().DisplayName;
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
                var transportId = handler.Request.WithData<TransportRequest>().NotNull().Data.NotNull().TransportId;
                var transport   = peer.Data().Transports.GetValueOrDefault(transportId);

                if (transport == null)
                    throw new KeyNotFoundException($"transport with id '{transportId}' not found");

                var stats = await transport.GetStatsAsync();

                await Accept(stats);

                break;
            }

            case "getProducerStats":
            {
                var producerId = handler.Request.WithData<ProducerRequest>().NotNull().Data.NotNull().ProducerId;

                var producer = peer.Data().Producers.GetValueOrDefault(producerId);

                if (producer == null)
                    throw new KeyNotFoundException($"producer with id '{producerId}' not found");

                var stats = await producer.GetStatsAsync();

                await Accept(stats);

                break;
            }

            case "getConsumerStats":
            {
                var consumerId = handler.Request.WithData<ConsumerRequest>().NotNull().Data.NotNull().ConsumerId;

                var consumer = peer.Data().Consumers.GetValueOrDefault(consumerId);

                if (consumer == null)
                    throw new KeyNotFoundException($"consumer with id '{consumerId}' not found");

                var stats = await consumer.GetStatsAsync();

                await Accept(stats);

                break;
            }

            case "getDataProducerStats":
            {
                var dataProducerId = handler.Request.WithData<DataProducerRequest>().NotNull().Data.NotNull().DataProducerId;

                var dataProducer = peer.Data().DataProducers.GetValueOrDefault(dataProducerId);

                if (dataProducer == null)
                    throw new KeyNotFoundException($"dataProducer with id '{dataProducerId}' not found");

                var stats = await dataProducer.GetStatsAsync();

                await Accept(stats);

                break;
            }

            case "getDataConsumerStats":
            {
                var dataConsumerId = handler.Request.WithData<DataConsumerRequest>().NotNull().Data.NotNull().DataConsumerId;

                var dataConsumer = peer.Data().DataConsumers.GetValueOrDefault(dataConsumerId);

                if (dataConsumer == null)
                    throw new KeyNotFoundException($"dataConsumer with id '{dataConsumerId}' not found");

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
                    handler.Request.WithData<ApplyNetworkThrottleRequest>().NotNull().Data.NotNull();

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

                    //TODO: watt is that
                    /*await throttle.start(new
                    {
                        up         = uplink     ?? DefaultUplink,
                        down       = downlink   ?? DefaultDownlink,
                        rtt        = rtt        ?? DefaultRtt,
                        packetLoss = packetLoss ?? DefaultPacketLoss
                    });*/

                    logger.LogWarning(
                        $"network throttle set [{nameof(uplink)}:{{Uplink}}, {nameof(downlink)}:{{Downlink}}, {nameof(rtt)}:{{Rtt}}, {nameof(packetLoss)}:{{PacketLoss}}]",
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
                var secret = handler.Request.WithData<ResetNetworkThrottleRequest>().NotNull().Data.NotNull().Secret;

                if (secret is not true ||
                    (bool.TryParse(Environment.GetEnvironmentVariable("NETWORK_THROTTLE_SECRET"), out var val) &&
                     val != secret))
                {
                    await Reject(403, "operation NOT allowed, modda fuckaa");

                    return;
                }

                try
                {
                    //TODO: watt is that
                    /*await throttle.stop({});*/

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
            .Where(x => x.Data().Joined && (excludePeer is null || x != excludePeer));

    private async Task CreateConsumerAsync(Peer consumerPeer, string producerPeerId, ProducerImpl<TWorkerAppData> producer)
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
        if (consumerPeer.Data().RtpCapabilities == null ||
            !await mediasoupRouter.CanConsumeAsync(producer.Id, consumerPeer.Data().RtpCapabilities.NotNull()))
        {
            return;
        }

        // Must take the Transport the remote Peer is using for consuming.
        var transport = consumerPeer.Data().Transports.Values.FirstOrDefault(static x => x.AppData()["consuming"] is true);

        // This should not happen.
        if (transport == null)
        {
            logger.LogWarning($"{nameof(CreateConsumerAsync)}() | Transport for consuming not found");

            return;
        }

        List<Task> tasks = [];

        var consumerCount = 1 + consumerReplicas;

        for (var i = 0; i < consumerCount; i++)
        {
            tasks.Add(Task.Run(async () =>
                {
                    // Create the Consumer in paused mode.
                    ConsumerImpl<TWorkerAppData>? consumer;

                    try
                    {
                        consumer = await transport.ConsumeAsync<TWorkerAppData>(new()
                        {
                            ProducerId      = producer.Id,
                            RtpCapabilities = consumerPeer.Data().RtpCapabilities.NotNull(),
                            // Enable NACK for OPUS.
                            EnableRtx = true,
                            Paused    = true,
                            IgnoreDtx = true
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
                    consumer.On(static x => x.TransportClose, () =>
                    {
                        // Remove from its map.
                        consumerPeer.Data().Consumers.Remove(consumer.Id);
                    });

                    consumer.On(static x => x.ProducerClose, () =>
                    {
                        // Remove from its map.
                        consumerPeer.Data().Consumers.Remove(consumer.Id);

                        consumerPeer.NotifyAsync("consumerClosed", new { consumerId = consumer.Id })
                            .Catch(() => { });
                    });

                    consumer.On(static x => x.ProducerPause, () =>
                    {
                        consumerPeer.NotifyAsync("consumerPaused", new { consumerId = consumer.Id })
                            .Catch(() => { });
                    });

                    consumer.On(static x => x.ProducerResume, () =>
                    {
                        consumerPeer.NotifyAsync("consumerResumed", new { consumerId = consumer.Id })
                            .Catch(() => { });
                    });

                    consumer.On(static x => x.Score, score =>
                    {
                        // logger.debug(
                        //	 'consumer "score" event [consumerId:%s, score:%o]',
                        //	 consumer.id, score);

                        consumerPeer.NotifyAsync("consumerScore", new { consumerId = consumer.Id, score })
                            .Catch(() => { });
                    });

                    consumer.On(static x => x.LayersChange, layers =>
                    {
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

                    consumer.On(static x => x.Trace, trace =>
                    {
                        logger.LogDebug(
                            $"consumer 'trace' event [producerId:{{ProducerId}}, trace.type:{{Type}}, {nameof(trace)}:{{Trace}}]",
                            consumer.Id, trace.Type, trace);
                    });

                    // Send a protoo request to the remote Peer with Consumer parameters.
                    try
                    {
                        await consumerPeer.RequestAsync(
                            "newConsumer", new
                            {
                                peerId         = producerPeerId,
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
                        logger.LogWarning($"{nameof(CreateConsumerAsync)}() | failed:{{Ex}}", ex);
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

    private async Task CreateDataConsumerAsync(
        Peer dataConsumerPeer,
        string? dataProducerPeerId, // This is null for the bot DataProducer.
        DataProducerImpl<TWorkerAppData> dataProducer)
    {
        // NOTE: Don't create the DataConsumer if the remote Peer cannot consume it.
        if (dataConsumerPeer.Data().SctpCapabilities == null)
            return;

        // Must take the Transport the remote Peer is using for consuming.
        var transport = dataConsumerPeer.Data().Transports.Values
            .FirstOrDefault(t => t.AppData()["consuming"] is true);

        // This should not happen.
        if (transport == null)
        {
            logger.LogWarning($"{nameof(CreateDataConsumerAsync)}() | Transport for consuming not found");

            return;
        }

        // Create the DataConsumer.
        DataConsumerImpl<TWorkerAppData> dataConsumer;

        try
        {
            dataConsumer = await transport.ConsumeDataAsync<TWorkerAppData>(
                new()
                {
                    DataProducerId = dataProducer.Id
                });
        }
        catch (Exception ex)
        {
            logger.LogWarning("CreateDataConsumer() | transport.consumeData():{Ex}", ex);

            return;
        }

        // Store the DataConsumer into the protoo dataConsumerPeer data Object.
        dataConsumerPeer.Data().DataConsumers.Add(dataConsumer.Id, dataConsumer);

        // Set DataConsumer events.
        dataConsumer.On(static x => x.TransportClose, () =>
        {
            // Remove from its map.
            dataConsumerPeer.Data().DataConsumers.Remove(dataConsumer.Id);
        });

        dataConsumer.On(static x => x.DataProducerClose, () =>
        {
            // Remove from its map.
            dataConsumerPeer.Data().DataConsumers.Remove(dataConsumer.Id);

            dataConsumerPeer.NotifyAsync(
                    "dataConsumerClosed", new { dataConsumerId = dataConsumer.Id })
                .Catch(() => { });
        });

        // Send a protoo request to the remote Peer with Consumer parameters.
        try
        {
            await dataConsumerPeer.RequestAsync(
                "newDataConsumer", new NewDataConsumerRequestR
                {
                    // This is null for bot DataProducer.
                    PeerId               = dataProducerPeerId,
                    DataProducerId       = dataProducer.Id,
                    Id                   = dataConsumer.Id,
                    SctpStreamParameters = dataConsumer.Data.SctpStreamParameters.NotNull(),
                    Label                = dataConsumer.Data.Label,
                    Protocol             = dataConsumer.Data.Protocol,
                    AppData              = dataProducer.AppData
                });
        }
        catch (Exception ex)
        {
            logger.LogWarning("CreateDataConsumer() | failed:{Ex}", ex);
        }
    }

    #region Definitions

    private class Broadcaster
    {
        public required string          Id   { get; set; }
        public required BroadcasterData Data { get; set; }
    }

    internal class BroadcasterData
    {
        public required string                                           DisplayName     { get; set; }
        public required DeviceR                                          Device          { get; set; }
        public          RtpCapabilities?                                 RtpCapabilities { get; set; }
        public          Dictionary<string, ITransport>                   Transports      { get; init; } = [];
        public          Dictionary<string, ProducerImpl<TWorkerAppData>>     Producers       { get; init; } = [];
        public          Dictionary<string, ConsumerImpl<TWorkerAppData>>     Consumers       { get; init; } = [];
        public          Dictionary<string, DataProducerImpl<TWorkerAppData>> DataProducers   { get; init; } = [];
        public          Dictionary<string, DataConsumerImpl<TWorkerAppData>> DataConsumers   { get; init; } = [];
    }

    internal class PeerData : BroadcasterData
    {
        public bool              Consume          { get; set; }
        public bool              Joined           { get; set; }
        public SctpCapabilities? SctpCapabilities { get; set; }
    }

    #endregion
}

file static class PeerExtensions
{
    public static Room.PeerData Data(this Peer peer) => peer.Data.As<Room.PeerData>();

    public static TWorkerAppData AppData(this ITransport transport) => transport switch
    {
        PlainTransportImpl<TWorkerAppData> plainTransport  => plainTransport.AppData,
        WebRtcTransportImpl<TWorkerAppData> plainTransport => plainTransport.AppData,
        DirectTransportImpl<TWorkerAppData> plainTransport => plainTransport.AppData,
        PipeTransportImpl<TWorkerAppData> plainTransport   => plainTransport.AppData,
        _                                              => throw new ArgumentOutOfRangeException(nameof(transport))
    };
}