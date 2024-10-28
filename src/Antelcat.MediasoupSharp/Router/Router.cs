using Antelcat.MediasoupSharp.ActiveSpeakerObserver;
using Antelcat.MediasoupSharp.AudioLevelObserver;
using Antelcat.MediasoupSharp.Channel;
using Antelcat.MediasoupSharp.Consumer;
using Antelcat.MediasoupSharp.DataConsumer;
using Antelcat.MediasoupSharp.DataProducer;
using Antelcat.MediasoupSharp.DirectTransport;
using Antelcat.MediasoupSharp.Exceptions;
using Antelcat.MediasoupSharp.PipeTransport;
using Antelcat.MediasoupSharp.PlainTransport;
using Antelcat.MediasoupSharp.Producer;
using Antelcat.MediasoupSharp.RtpObserver;
using Antelcat.MediasoupSharp.RtpParameters;
using Antelcat.MediasoupSharp.Transport;
using Antelcat.MediasoupSharp.WebRtcTransport;
using FBS.Request;
using FBS.Router;
using Antelcat.MediasoupSharp.Internals.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Threading;

namespace Antelcat.MediasoupSharp.Router;

public sealed class Router : EnhancedEvent.EnhancedEventEmitter, IEquatable<Router>
{
    /// <summary>
    /// Logger factory for create logger.
    /// </summary>
    private readonly ILoggerFactory loggerFactory;

    /// <summary>
    /// Logger.
    /// </summary>
    private readonly ILogger<Router> logger;

    /// <summary>
    /// Whether the Router is closed.
    /// </summary>
    private bool closed;

    /// <summary>
    /// Close locker.
    /// </summary>
    private readonly AsyncReaderWriterLock closeLock = new();

    #region Internal data.

    private readonly RouterInternal @internal;

    public string Id => @internal.RouterId;

    #endregion Internal data.

    #region Router data.

    public RouterData Data { get; }

    #endregion Router data.

    /// <summary>
    /// Channel instance.
    /// </summary>
    private readonly IChannel channel;

    /// <summary>
    /// Transports map.
    /// </summary>
    private readonly Dictionary<string, Transport.Transport> transports = new();

    private readonly AsyncReaderWriterLock transportsLock = new();

    /// <summary>
    /// Producers map.
    /// </summary>
    private readonly Dictionary<string, Producer.Producer> producers = new();

    private readonly AsyncReaderWriterLock producersLock = new();

    /// <summary>
    /// RtpObservers map.
    /// </summary>
    private readonly Dictionary<string, RtpObserver.RtpObserver> rtpObservers = new();

    private readonly AsyncReaderWriterLock rtpObserversLock = new();

    /// <summary>
    /// DataProducers map.
    /// </summary>
    private readonly Dictionary<string, DataProducer.DataProducer> dataProducers = new();

    private readonly AsyncReaderWriterLock dataProducersLock = new();

    /// <summary>
    /// Router to PipeTransport map.
    /// </summary>
    private readonly Dictionary<Router, PipeTransport.PipeTransport[]> mapRouterPipeTransports = new();

    private readonly AsyncReaderWriterLock mapRouterPipeTransportsLock = new();

    /// <summary>
    /// App custom data.
    /// </summary>
    public AppData AppData { get; }

    /// <summary>
    /// Observer instance.
    /// </summary>
    public EnhancedEvent.EnhancedEventEmitter Observer { get; } = new();

    /// <summary>
    /// <para>Events:</para>
    /// <para>@emits workerclose</para>
    /// <para>@emits @close</para>
    /// <para>Observer events:</para>
    /// <para>@emits close</para>
    /// <para>@emits newtransport - (transport: Transport)</para>
    /// <para>@emits newrtpobserver - (rtpObserver: RtpObserver)</para>
    /// </summary>
    public Router(ILoggerFactory loggerFactory,
                  RouterInternal @internal,
                  RouterData data,
                  IChannel channel,
                  AppData? appData
    )
    {
        this.loggerFactory = loggerFactory;
        logger             = loggerFactory.CreateLogger<Router>();
        this.@internal     = @internal;
        this.channel       = channel;
        Data               = data;
        AppData            = appData ?? new Dictionary<string, object>();
    }

    /// <summary>
    /// Close the Router.
    /// </summary>
    public async Task CloseAsync()
    {
        logger.LogDebug("CloseAsync() | Router:{RouterId}", Id);

        await using (await closeLock.WriteLockAsync())
        {
            if (closed)
            {
                return;
            }

            closed = true;

            // Build Request
            var bufferBuilder = channel.BufferPool.Get();

            var closeRouterRequest = new FBS.Worker.CloseRouterRequestT
            {
                RouterId = @internal.RouterId,
            };

            var closeRouterRequestOffset = FBS.Worker.CloseRouterRequest.Pack(bufferBuilder, closeRouterRequest);

            // Fire and forget
            channel.RequestAsync(bufferBuilder, Method.WORKER_CLOSE_ROUTER,
                Body.Worker_CloseRouterRequest,
                closeRouterRequestOffset.Value
            ).ContinueWithOnFaultedHandleLog(logger);

            await CloseInternalAsync();

            Emit("@close");

            // Emit observer event.
            Observer.Emit("close");
        }
    }

    /// <summary>
    /// Worker was closed.
    /// </summary>
    public async Task WorkerClosedAsync()
    {
        logger.LogDebug("WorkerClosedAsync() | Router:{RouterId}", Id);

        await using (await closeLock.WriteLockAsync())
        {
            if (closed)
            {
                return;
            }

            closed = true;

            await CloseInternalAsync();

            Emit("workerclose");

            // Emit observer event.
            Observer.Emit("close");
        }
    }

    /// <summary>
    /// Dump Router.
    /// </summary>
    public async Task<DumpResponseT> DumpAsync()
    {
        logger.LogDebug("DumpAsync() | Router:{RouterId}", Id);

        await using (await closeLock.ReadLockAsync())
        {
            if (closed)
            {
                throw new InvalidStateException("Router closed");
            }

            // Build Request
            var bufferBuilder = channel.BufferPool.Get();

            var response =
                await channel.RequestAsync(bufferBuilder, Method.ROUTER_DUMP, null, null, @internal.RouterId);
            var data = response!.Value.BodyAsRouter_DumpResponse().UnPack();

            return data;
        }
    }

    /// <summary>
    /// Create a WebRtcTransport.
    /// </summary>
    public async Task<WebRtcTransport.WebRtcTransport> CreateWebRtcTransportAsync(
        Antelcat.MediasoupSharp.WebRtcTransport.WebRtcTransportOptions options)
    {
        var (webRtcServer,
            listenInfos,
            enableUdp,
            enableTcp,
            preferUdp,
            preferTcp,
            initialAvailableOutgoingBitrate,
            enableSctp,
            numSctpStreams,
            maxSctpMessageSize,
            sctpSendBufferSize,
            iceConsentTimeout,
            appData) = options;
        logger.LogDebug("CreateWebRtcTransportAsync()");

        if (webRtcServer == null && listenInfos.IsNullOrEmpty())
        {
            throw new ArgumentException("missing webRtcServer and listenIps (one of them is mandatory)");
        }
        /*
        else if(webRtcTransportOptions.WebRtcServer != null && !webRtcTransportOptions.ListenInfos.IsNullOrEmpty())
        {
            throw new ArgumentException("only one of webRtcServer, listenInfos and listenIps must be given");
        }
        */


        // If webRtcServer is given, then do not force default values for enableUdp
        // and enableTcp. Otherwise set them if unset.
        if (webRtcServer != null)
        {
            enableUdp ??= true;
            enableTcp ??= true;
        }
        else
        {
            enableUdp ??= true;
            enableTcp ??= false;
        }

        await using (await closeLock.ReadLockAsync())
        {
            if (closed)
            {
                throw new InvalidStateException("Router closed");
            }

            /* Build Request. */
            FBS.WebRtcTransport.ListenServerT?     webRtcTransportListenServer     = null;
            FBS.WebRtcTransport.ListenIndividualT? webRtcTransportListenIndividual = null;
            if (webRtcServer != null)
            {
                webRtcTransportListenServer = new FBS.WebRtcTransport.ListenServerT
                {
                    WebRtcServerId = webRtcServer.Id
                };
            }
            else
            {
                var fbsListenInfos = listenInfos!
                    .Select(static m => new FBS.Transport.ListenInfoT
                    {
                        Protocol         = m.Protocol,
                        Ip               = m.Ip,
                        AnnouncedAddress = m.AnnouncedAddress,
                        Port             = m.Port,
                        PortRange        = m.PortRange,
                        Flags            = m.Flags,
                        SendBufferSize   = m.SendBufferSize,
                        RecvBufferSize   = m.RecvBufferSize,
                    }).ToList();

                webRtcTransportListenIndividual =
                    new FBS.WebRtcTransport.ListenIndividualT
                    {
                        ListenInfos = fbsListenInfos,
                    };
            }

            var baseTransportOptions = new FBS.Transport.OptionsT
            {
                Direct                          = false,
                MaxMessageSize                  = null,
                InitialAvailableOutgoingBitrate = initialAvailableOutgoingBitrate,
                EnableSctp                      = enableSctp,
                NumSctpStreams                  = numSctpStreams,
                MaxSctpMessageSize              = maxSctpMessageSize,
                SctpSendBufferSize              = sctpSendBufferSize,
                IsDataChannel                   = true
            };

            var webRtcTransportOptions = new FBS.WebRtcTransport.WebRtcTransportOptionsT
            {
                Base = baseTransportOptions,
                Listen = new FBS.WebRtcTransport.ListenUnion
                {
                    Type = webRtcServer != null
                        ? FBS.WebRtcTransport.Listen.ListenServer
                        : FBS.WebRtcTransport.Listen.ListenIndividual,
                    Value = webRtcServer != null ? webRtcTransportListenServer : webRtcTransportListenIndividual
                },
                EnableUdp         = enableUdp.Value,
                EnableTcp         = enableTcp.Value,
                PreferUdp         = preferUdp,
                PreferTcp         = preferTcp,
                IceConsentTimeout = iceConsentTimeout,
            };

            var transportId = Guid.NewGuid().ToString();

            // Build Request
            var bufferBuilder = channel.BufferPool.Get();

            var createWebRtcTransportRequest = new CreateWebRtcTransportRequestT
            {
                TransportId = transportId,
                Options     = webRtcTransportOptions
            };

            var createWebRtcTransportRequestOffset =
                CreateWebRtcTransportRequest.Pack(bufferBuilder, createWebRtcTransportRequest);

            var response = await channel.RequestAsync(bufferBuilder, webRtcServer != null
                    ? Method.ROUTER_CREATE_WEBRTCTRANSPORT_WITH_SERVER
                    : Method.ROUTER_CREATE_WEBRTCTRANSPORT,
                Body.Router_CreateWebRtcTransportRequest,
                createWebRtcTransportRequestOffset.Value,
                @internal.RouterId);

            /* Decode Response. */
            var data = response!.Value.BodyAsWebRtcTransport_DumpResponse().UnPack();

            var transport = new WebRtcTransport.WebRtcTransport(loggerFactory,
                new TransportInternal(@internal.RouterId, transportId),
                data, // 直接使用返回值
                channel,
                appData,
                () => Data.RtpCapabilities,
                async m =>
                {
                    await using (await producersLock.ReadLockAsync())
                    {
                        return producers.GetValueOrDefault(m);
                    }
                },
                async m =>
                {
                    await using (await dataProducersLock.ReadLockAsync())
                    {
                        return dataProducers.GetValueOrDefault(m);
                    }
                }
            );

            await ConfigureTransportAsync(transport, webRtcServer);

            return transport;
        }
    }

    /// <summary>
    /// Create a PlainTransport.
    /// </summary>
    public async Task<PlainTransport.PlainTransport> CreatePlainTransportAsync(
        Antelcat.MediasoupSharp.PlainTransport.PlainTransportOptions options)
    {
        logger.LogDebug("CreatePlainTransportAsync()");

        await using (await closeLock.ReadLockAsync())
        {
            if (closed)
                throw new InvalidStateException("Router closed");

            if (options.ListenInfo?.Ip.IsNullOrWhiteSpace() != false)
                throw new ArgumentException("Missing ListenInfo");

            // If rtcpMux is enabled, ignore rtcpListenInfo.
            if (options is { RtcpMux: true, RtcpListenInfo: not null })
            {
                logger.LogWarning("createPlainTransport() | ignoring rtcpMux since rtcpListenInfo is given");
                options.RtcpMux = false;
            }

            var baseTransportOptions = new FBS.Transport.OptionsT
            {
                Direct                          = false,
                MaxMessageSize                  = null,
                InitialAvailableOutgoingBitrate = null,
                EnableSctp                      = options.EnableSctp,
                NumSctpStreams                  = options.NumSctpStreams,
                MaxSctpMessageSize              = options.MaxSctpMessageSize,
                SctpSendBufferSize              = options.SctpSendBufferSize,
                IsDataChannel                   = false
            };

            var plainTransportOptions = new FBS.PlainTransport.PlainTransportOptionsT
            {
                Base            = baseTransportOptions,
                ListenInfo      = options.ListenInfo,
                RtcpListenInfo  = options.RtcpListenInfo,
                RtcpMux         = options.RtcpMux,
                Comedia         = options.Comedia,
                EnableSrtp      = options.EnableSrtp,
                SrtpCryptoSuite = options.SrtpCryptoSuite
            };

            var transportId = Guid.NewGuid().ToString();

            // Build Request
            var bufferBuilder = channel.BufferPool.Get();

            var createPlainTransportRequest = new CreatePlainTransportRequestT
            {
                TransportId = transportId,
                Options     = plainTransportOptions
            };

            var requestOffset = CreatePlainTransportRequest.Pack(bufferBuilder, createPlainTransportRequest);

            var response = await channel.RequestAsync(bufferBuilder,
                Method.ROUTER_CREATE_PLAINTRANSPORT,
                Body.Router_CreatePlainTransportRequest,
                requestOffset.Value,
                @internal.RouterId);

            /* Decode Response. */
            var data = response.Value.BodyAsPlainTransport_DumpResponse().UnPack();

            var transport = new PlainTransport.PlainTransport(loggerFactory,
                new TransportInternal(@internal.RouterId, transportId),
                data, // 直接使用返回值
                channel,
                options.AppData,
                () => Data.RtpCapabilities,
                async m =>
                {
                    await using (await producersLock.ReadLockAsync())
                    {
                        return producers.GetValueOrDefault(m);
                    }
                },
                async m =>
                {
                    await using (await dataProducersLock.ReadLockAsync())
                    {
                        return dataProducers.GetValueOrDefault(m);
                    }
                }
            );

            await ConfigureTransportAsync(transport);

            return transport;
        }
    }

    /// <summary>
    /// Create a PipeTransport.
    /// </summary>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="InvalidStateException"></exception>
    public async Task<PipeTransport.PipeTransport> CreatePipeTransportAsync(PipeTransportOptions options)
    {
        logger.LogDebug("CreatePipeTransportAsync()");

        await using (await closeLock.ReadLockAsync())
        {
            if (closed)
            {
                throw new InvalidStateException("Router closed");
            }

            if (options.ListenInfo?.Ip.IsNullOrWhiteSpace() != false)
            {
                throw new ArgumentException("Missing ListenInfo");
            }

            var baseTransportOptions = new FBS.Transport.OptionsT
            {
                Direct                          = false,
                MaxMessageSize                  = null,
                InitialAvailableOutgoingBitrate = null,
                EnableSctp                      = options.EnableSctp,
                NumSctpStreams                  = options.NumSctpStreams,
                MaxSctpMessageSize              = options.MaxSctpMessageSize,
                SctpSendBufferSize              = options.SctpSendBufferSize,
                IsDataChannel                   = false
            };

            var listenInfo = options.ListenInfo;

            var pipeTransportOptions = new FBS.PipeTransport.PipeTransportOptionsT
            {
                Base       = baseTransportOptions,
                ListenInfo = options.ListenInfo,
                EnableRtx  = options.EnableRtx,
                EnableSrtp = options.EnableSrtp,
            };

            var transportId = Guid.NewGuid().ToString();

            // Build Request
            var bufferBuilder = channel.BufferPool.Get();

            var createPipeTransportRequest = new CreatePipeTransportRequestT
            {
                TransportId = transportId,
                Options     = pipeTransportOptions
            };

            var createPipeTransportRequestOffset =
                CreatePipeTransportRequest.Pack(bufferBuilder, createPipeTransportRequest);

            var response = await channel.RequestAsync(bufferBuilder, Method.ROUTER_CREATE_PIPETRANSPORT,
                Body.Router_CreatePipeTransportRequest,
                createPipeTransportRequestOffset.Value,
                @internal.RouterId);

            /* Decode Response. */
            var data = response.Value.BodyAsPipeTransport_DumpResponse().UnPack();

            var transport = new PipeTransport.PipeTransport(loggerFactory,
                new TransportInternal(@internal.RouterId, transportId),
                data, // 直接使用返回值
                channel,
                options.AppData,
                () => Data.RtpCapabilities,
                async m =>
                {
                    await using (await producersLock.ReadLockAsync())
                    {
                        return producers.GetValueOrDefault(m);
                    }
                },
                async m =>
                {
                    await using (await dataProducersLock.ReadLockAsync())
                    {
                        return dataProducers.GetValueOrDefault(m);
                    }
                });

            await ConfigureTransportAsync(transport);

            return transport;
        }
    }

    /// <summary>
    /// Create a DirectTransport.
    /// </summary>
    public async Task<DirectTransport.DirectTransport> CreateDirectTransportAsync(
        DirectTransportOptions directTransportOptions)
    {
        logger.LogDebug("CreateDirectTransportAsync()");

        await using (await closeLock.ReadLockAsync())
        {
            if (closed)
            {
                throw new InvalidStateException("Router closed");
            }

            var baseTransportOptions = new FBS.Transport.OptionsT
            {
                Direct         = true,
                MaxMessageSize = directTransportOptions.MaxMessageSize,
            };

            var reqData = new
            {
                TransportId = Guid.NewGuid().ToString(),
                Direct      = true,
                directTransportOptions.MaxMessageSize,
            };

            var directTransportOptionsForCreate = new FBS.DirectTransport.DirectTransportOptionsT
            {
                Base = baseTransportOptions,
            };

            var transportId = Guid.NewGuid().ToString();

            // Build Request
            var bufferBuilder = channel.BufferPool.Get();

            var createDirectTransportRequest = new CreateDirectTransportRequestT
            {
                TransportId = transportId,
                Options     = directTransportOptionsForCreate
            };

            var createDirectTransportRequestOffset =
                CreateDirectTransportRequest.Pack(bufferBuilder, createDirectTransportRequest);

            var response = await channel.RequestAsync(bufferBuilder, Method.ROUTER_CREATE_DIRECTTRANSPORT,
                Body.Router_CreateDirectTransportRequest,
                createDirectTransportRequestOffset.Value,
                @internal.RouterId);

            /* Decode Response. */
            var data = response.Value.BodyAsDirectTransport_DumpResponse().UnPack();

            var transport = new DirectTransport.DirectTransport(loggerFactory,
                new TransportInternal(Id, transportId),
                data, // 直接使用返回值
                channel,
                directTransportOptions.AppData,
                () => Data.RtpCapabilities,
                async m =>
                {
                    await using (await producersLock.ReadLockAsync())
                    {
                        return producers.GetValueOrDefault(m);
                    }
                },
                async m =>
                {
                    await using (await dataProducersLock.ReadLockAsync())
                    {
                        return dataProducers.GetValueOrDefault(m);
                    }
                }
            );

            await ConfigureTransportAsync(transport);

            return transport;
        }
    }

    private async Task ConfigureTransportAsync(Transport.Transport transport,
                                               WebRtcServer.WebRtcServer? webRtcServer = null)
    {
        await using (await transportsLock.WriteLockAsync())
        {
            transports[transport.Id] = transport;
        }

        transport.On("@close", async _ =>
        {
            await using (await transportsLock.WriteLockAsync())
            {
                transports.Remove(transport.Id);
            }
        });
        transport.On("@listenserverclose", async _ =>
        {
            await using (await transportsLock.WriteLockAsync())
            {
                transports.Remove(transport.Id);
            }
        });
        transport.On("@newproducer", async obj =>
        {
            var producer = (Producer.Producer)obj!;
            await using (await producersLock.WriteLockAsync())
            {
                producers[producer.Id] = producer;
            }
        });
        transport.On("@producerclose", async obj =>
        {
            var producer = (Producer.Producer)obj!;
            await using (await producersLock.WriteLockAsync())
            {
                producers.Remove(producer.Id);
            }
        });
        transport.On("@newdataproducer", async obj =>
        {
            var dataProducer = (DataProducer.DataProducer)obj!;
            await using (await dataProducersLock.WriteLockAsync())
            {
                dataProducers[dataProducer.Id] = dataProducer;
            }
        });
        transport.On("@dataproducerclose", async obj =>
        {
            var dataProducer = (DataProducer.DataProducer)obj!;
            await using (await dataProducersLock.WriteLockAsync())
            {
                dataProducers.Remove(dataProducer.Id);
            }
        });

        // Emit observer event.
        Observer.Emit("newtransport", transport);

        if (webRtcServer != null && transport is WebRtcTransport.WebRtcTransport webRtcTransport)
        {
            await webRtcServer.HandleWebRtcTransportAsync(webRtcTransport);
        }
    }

    /// <summary>
    /// Pipes the given Producer or DataProducer into another Router in same host.
    /// </summary>
    /// <param name="pipeToRouterOptions">ListenIp 传入 127.0.0.1, EnableSrtp 传入 true 。</param>
    ///
    public async Task<PipeToRouterResult> PipeToRouteAsync(PipeToRouterOptions pipeToRouterOptions)
    {
        await using (await closeLock.ReadLockAsync())
        {
            if (closed)
            {
                throw new InvalidStateException("Router closed");
            }

            if (pipeToRouterOptions.ListenInfo == null)
            {
                throw new ArgumentNullException(nameof(pipeToRouterOptions), "Missing listenInfo");
            }

            if (pipeToRouterOptions.ProducerId.IsNullOrWhiteSpace() &&
                pipeToRouterOptions.DataProducerId.IsNullOrWhiteSpace())
            {
                throw new ArgumentException("Missing producerId or dataProducerId");
            }

            if (!pipeToRouterOptions.ProducerId.IsNullOrWhiteSpace() &&
                !pipeToRouterOptions.DataProducerId.IsNullOrWhiteSpace())
            {
                throw new ArgumentException("Just producerId or dataProducerId can be given");
            }

            if (pipeToRouterOptions.Router == null)
            {
                throw new ArgumentNullException(nameof(pipeToRouterOptions), "Router not found");
            }

            if (pipeToRouterOptions.Router == this)
            {
                throw new ArgumentException("Cannot use this Router as destination");
            }

            Producer.Producer?         producer     = null;
            DataProducer.DataProducer? dataProducer = null;

            if (!pipeToRouterOptions.ProducerId.IsNullOrWhiteSpace())
            {
                await using (await producersLock.ReadLockAsync())
                {
                    if (!producers.TryGetValue(pipeToRouterOptions.ProducerId!, out producer))
                    {
                        throw new Exception("Producer not found");
                    }
                }
            }
            else if (!pipeToRouterOptions.DataProducerId.IsNullOrWhiteSpace())
            {
                await using (await dataProducersLock.ReadLockAsync())
                {
                    if (!dataProducers.TryGetValue(pipeToRouterOptions.DataProducerId!, out dataProducer))
                    {
                        throw new Exception("DataProducer not found");
                    }
                }
            }

            // Here we may have to create a new PipeTransport pair to connect source and
            // destination Routers. We just want to keep a PipeTransport pair for each
            // pair of Routers. Since this operation is async, it may happen that two
            // simultaneous calls to router1.pipeToRouter({ producerId: xxx, router: router2 })
            // would end up generating two pairs of PipeTranports. To prevent that, let's
            // use an async queue.

            PipeTransport.PipeTransport? localPipeTransport  = null;
            PipeTransport.PipeTransport? remotePipeTransport = null;

            // 因为有可能新增，所以用写锁。
            await using (await mapRouterPipeTransportsLock.WriteLockAsync())
            {
                if (mapRouterPipeTransports.TryGetValue(pipeToRouterOptions.Router, out var pipeTransportPair))
                {
                    localPipeTransport  = pipeTransportPair[0];
                    remotePipeTransport = pipeTransportPair[1];
                }
                else
                {
                    try
                    {
                        var pipeTransports = await Task.WhenAll(CreatePipeTransportAsync(new PipeTransportOptions
                            {
                                ListenInfo     = pipeToRouterOptions.ListenInfo,
                                EnableSctp     = pipeToRouterOptions.EnableSctp,
                                NumSctpStreams = pipeToRouterOptions.NumSctpStreams,
                                EnableRtx      = pipeToRouterOptions.EnableRtx,
                                EnableSrtp     = pipeToRouterOptions.EnableSrtp
                            }),
                            pipeToRouterOptions.Router.CreatePipeTransportAsync(new PipeTransportOptions
                            {
                                ListenInfo     = pipeToRouterOptions.ListenInfo,
                                EnableSctp     = pipeToRouterOptions.EnableSctp,
                                NumSctpStreams = pipeToRouterOptions.NumSctpStreams,
                                EnableRtx      = pipeToRouterOptions.EnableRtx,
                                EnableSrtp     = pipeToRouterOptions.EnableSrtp
                            })
                        );

                        localPipeTransport  = pipeTransports[0];
                        remotePipeTransport = pipeTransports[1];

                        await Task.WhenAll(localPipeTransport.ConnectAsync(new FBS.PipeTransport.ConnectRequestT
                            {
                                Ip             = remotePipeTransport.Data.Tuple.LocalAddress,
                                Port           = remotePipeTransport.Data.Tuple.LocalPort,
                                SrtpParameters = remotePipeTransport.Data.SrtpParameters,
                            }),
                            remotePipeTransport.ConnectAsync(new FBS.PipeTransport.ConnectRequestT
                            {
                                Ip             = localPipeTransport.Data.Tuple.LocalAddress,
                                Port           = localPipeTransport.Data.Tuple.LocalPort,
                                SrtpParameters = localPipeTransport.Data.SrtpParameters,
                            })
                        );

                        localPipeTransport.Observer.On("close", async _ =>
                        {
                            await remotePipeTransport.CloseAsync();
                            await using (await mapRouterPipeTransportsLock.WriteLockAsync())
                            {
                                mapRouterPipeTransports.Remove(pipeToRouterOptions.Router);
                            }
                        });

                        remotePipeTransport.Observer.On("close", async _ =>
                        {
                            await localPipeTransport.CloseAsync();
                            await using (await mapRouterPipeTransportsLock.WriteLockAsync())
                            {
                                mapRouterPipeTransports.Remove(pipeToRouterOptions.Router);
                            }
                        });

                        mapRouterPipeTransports[pipeToRouterOptions.Router] =
                        [
                            localPipeTransport, remotePipeTransport
                        ];
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "PipeToRouterAsync() | Create PipeTransport pair failed.");

                        if (localPipeTransport != null)
                        {
                            await localPipeTransport.CloseAsync();
                        }

                        if (remotePipeTransport != null)
                        {
                            await remotePipeTransport.CloseAsync();
                        }

                        throw;
                    }
                }
            }

            if (producer != null)
            {
                Consumer.Consumer? pipeConsumer = null;
                Producer.Producer? pipeProducer = null;

                try
                {
                    pipeConsumer = await localPipeTransport.ConsumeAsync(new ConsumerOptions
                    {
                        ProducerId = pipeToRouterOptions.ProducerId!
                    });

                    pipeProducer = await remotePipeTransport.ProduceAsync(new ProducerOptions
                    {
                        Id            = producer.Id,
                        Kind          = pipeConsumer.Data.Kind,
                        RtpParameters = pipeConsumer.Data.RtpParameters,
                        Paused        = pipeConsumer.ProducerPaused,
                        AppData       = producer.AppData,
                    });

                    // Ensure that the producer has not been closed in the meanwhile.
                    if (producer.Closed)
                        throw new InvalidStateException("original Producer closed");

                    // Ensure that producer.paused has not changed in the meanwhile and, if
                    // so, sync the pipeProducer.
                    if (pipeProducer.Paused != producer.Paused)
                    {
                        if (producer.Paused)
                            await pipeProducer.PauseAsync();
                        else
                            await pipeProducer.ResumeAsync();
                    }

                    // Pipe events from the pipe Consumer to the pipe Producer.
                    pipeConsumer.Observer.On("close", async _ => await pipeProducer.CloseAsync());
                    pipeConsumer.Observer.On("pause", async _ => await pipeProducer.PauseAsync());
                    pipeConsumer.Observer.On("resume", async _ => await pipeProducer.ResumeAsync());

                    // Pipe events from the pipe Producer to the pipe Consumer.
                    pipeProducer.Observer.On("close", async _ => await pipeConsumer.CloseAsync());

                    return new PipeToRouterResult { PipeConsumer = pipeConsumer, PipeProducer = pipeProducer };
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "PipeToRouterAsync() | Create pipe Consumer/Producer pair failed");

                    if (pipeConsumer != null)
                    {
                        await pipeConsumer.CloseAsync();
                    }

                    if (pipeProducer != null)
                    {
                        await pipeProducer.CloseAsync();
                    }

                    throw;
                }
            }

            if (dataProducer != null)
            {
                DataConsumer.DataConsumer? pipeDataConsumer = null;
                DataProducer.DataProducer? pipeDataProducer = null;

                try
                {
                    pipeDataConsumer = await localPipeTransport.ConsumeDataAsync(new DataConsumerOptions
                    {
                        DataProducerId = pipeToRouterOptions.DataProducerId!
                    });

                    pipeDataProducer = await remotePipeTransport.ProduceDataAsync(new DataProducerOptions
                    {
                        Id                   = dataProducer.Id,
                        SctpStreamParameters = pipeDataConsumer.Data.SctpStreamParameters,
                        Label                = pipeDataConsumer.Data.Label,
                        Protocol             = pipeDataConsumer.Data.Protocol,
                        AppData              = dataProducer.AppData,
                    });

                    // Pipe events from the pipe DataConsumer to the pipe DataProducer.
                    pipeDataConsumer.Observer.On("close", async _ => await pipeDataProducer.CloseAsync());

                    // Pipe events from the pipe DataProducer to the pipe DataConsumer.
                    pipeDataProducer.Observer.On("close", async _ => await pipeDataConsumer.CloseAsync());

                    return new PipeToRouterResult
                        { PipeDataConsumer = pipeDataConsumer, PipeDataProducer = pipeDataProducer };
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "PipeToRouterAsync() | Create pipe DataConsumer/DataProducer pair failed.");

                    if (pipeDataConsumer != null)
                    {
                        await pipeDataConsumer.CloseAsync();
                    }

                    if (pipeDataProducer != null)
                    {
                        await pipeDataProducer.CloseAsync();
                    }

                    throw;
                }
            }

            throw new Exception("Internal error");
        }
    }

    /// <summary>
    /// Create an ActiveSpeakerObserver
    /// </summary>
    public async Task<ActiveSpeakerObserver.ActiveSpeakerObserver> CreateActiveSpeakerObserverAsync(
        ActiveSpeakerObserverOptions activeSpeakerObserverOptions)
    {
        logger.LogDebug("CreateActiveSpeakerObserverAsync()");

        await using (await closeLock.ReadLockAsync())
        {
            if (closed)
            {
                throw new InvalidStateException("Router closed");
            }

            var rtpObserverId = Guid.NewGuid().ToString();

            // Build Request
            var bufferBuilder = channel.BufferPool.Get();

            var createActiveSpeakerObserverRequest = new CreateActiveSpeakerObserverRequestT
            {
                RtpObserverId = rtpObserverId,
                Options = new FBS.ActiveSpeakerObserver.ActiveSpeakerObserverOptionsT
                {
                    Interval = activeSpeakerObserverOptions.Interval,
                }
            };

            var createActiveSpeakerObserverRequestOffset =
                CreateActiveSpeakerObserverRequest.Pack(bufferBuilder, createActiveSpeakerObserverRequest);

            // Fire and forget
            channel.RequestAsync(bufferBuilder, Method.ROUTER_CREATE_ACTIVESPEAKEROBSERVER,
                Body.Router_CreateActiveSpeakerObserverRequest,
                createActiveSpeakerObserverRequestOffset.Value,
                @internal.RouterId
            ).ContinueWithOnFaultedHandleLog(logger);

            var activeSpeakerObserver = new ActiveSpeakerObserver.ActiveSpeakerObserver(loggerFactory,
                new RtpObserverInternal(@internal.RouterId, rtpObserverId),
                channel,
                activeSpeakerObserverOptions.AppData,
                async m =>
                {
                    await using (await producersLock.ReadLockAsync())
                    {
                        return producers.GetValueOrDefault(m);
                    }
                });

            await ConfigureRtpObserverAsync(activeSpeakerObserver);

            return activeSpeakerObserver;
        }
    }

    /// <summary>
    /// Create an AudioLevelObserver.
    /// </summary>
    public async Task<AudioLevelObserver.AudioLevelObserver> CreateAudioLevelObserverAsync(
        AudioLevelObserverOptions audioLevelObserverOptions)
    {
        logger.LogDebug("CreateAudioLevelObserverAsync()");

        await using (await closeLock.ReadLockAsync())
        {
            if (closed)
            {
                throw new InvalidStateException("Router closed");
            }

            var rtpObserverId = Guid.NewGuid().ToString();

            // Build Request
            var bufferBuilder = channel.BufferPool.Get();

            var createAudioLevelObserverRequest = new CreateAudioLevelObserverRequestT
            {
                RtpObserverId = rtpObserverId,
                Options = new FBS.AudioLevelObserver.AudioLevelObserverOptionsT
                {
                    MaxEntries = audioLevelObserverOptions.MaxEntries,
                    Threshold  = audioLevelObserverOptions.Threshold,
                    Interval   = audioLevelObserverOptions.Interval,
                }
            };

            var createAudioLevelObserverRequestOffset =
                CreateAudioLevelObserverRequest.Pack(bufferBuilder, createAudioLevelObserverRequest);

            // Fire and forget
            channel.RequestAsync(bufferBuilder, Method.ROUTER_CREATE_AUDIOLEVELOBSERVER,
                Body.Router_CreateAudioLevelObserverRequest,
                createAudioLevelObserverRequestOffset.Value,
                @internal.RouterId
            ).ContinueWithOnFaultedHandleLog(logger);

            var audioLevelObserver = new AudioLevelObserver.AudioLevelObserver(loggerFactory,
                new RtpObserverInternal(@internal.RouterId, rtpObserverId),
                channel,
                audioLevelObserverOptions.AppData,
                async m =>
                {
                    await using (await producersLock.ReadLockAsync())
                    {
                        return producers.GetValueOrDefault(m);
                    }
                });

            await ConfigureRtpObserverAsync(audioLevelObserver);

            return audioLevelObserver;
        }
    }

    /// <summary>
    /// Check whether the given RTP capabilities can consume the given Producer.
    /// </summary>
    public async Task<bool> CanConsumeAsync(string producerId, RtpCapabilities rtpCapabilities)
    {
        await using (await producersLock.ReadLockAsync())
        {
            if (!producers.TryGetValue(producerId, out var producer))
            {
                logger.LogError("CanConsume() | Producer with id {producerId} not found", producerId);
                return false;
            }

            try
            {
                return ORTC.Ortc.CanConsume(producer.Data.ConsumableRtpParameters, rtpCapabilities);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "CanConsume() | Unexpected error");
                return false;
            }
        }
    }

    #region IEquatable<T>

    public bool Equals(Router? other)
    {
        if (other is null)
        {
            return false;
        }

        return Id == other.Id;
    }

    public override bool Equals(object? other)
    {
        return Equals(other as Router);
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }

    #endregion IEquatable<T>

    private async Task CloseInternalAsync()
    {
        await using (await transportsLock.WriteLockAsync())
        {
            // Close every Transport.
            foreach (var transport in transports.Values)
            {
                await transport.RouterClosedAsync();
            }

            transports.Clear();
        }

        await using (await producersLock.WriteLockAsync())
        {
            // Clear the Producers map.
            producers.Clear();
        }

        await using (await rtpObserversLock.WriteLockAsync())
        {
            // Close every RtpObserver.
            foreach (var rtpObserver in rtpObservers.Values)
            {
                await rtpObserver.RouterClosedAsync();
            }

            rtpObservers.Clear();
        }

        await using (await dataProducersLock.WriteLockAsync())
        {
            // Clear the DataProducers map.
            dataProducers.Clear();
        }

        await using (await mapRouterPipeTransportsLock.WriteLockAsync())
        {
            // Clear map of Router/PipeTransports.
            mapRouterPipeTransports.Clear();
        }
    }

    private Task ConfigureRtpObserverAsync(RtpObserver.RtpObserver rtpObserver)
    {
        rtpObservers[rtpObserver.Internal.RtpObserverId] = rtpObserver;
        rtpObserver.On("@close", async _ =>
        {
            await using (await rtpObserversLock.WriteLockAsync())
            {
                rtpObservers.Remove(rtpObserver.Internal.RtpObserverId);
            }
        });

        // Emit observer event.
        Observer.Emit("newrtpobserver", rtpObserver);

        return Task.CompletedTask;
    }
}