using MediasoupSharp.ActiveSpeakerObserver;
using MediasoupSharp.AudioLevelObserver;
using MediasoupSharp.Channel;
using MediasoupSharp.Consumer;
using MediasoupSharp.DataConsumer;
using MediasoupSharp.DataProducer;
using MediasoupSharp.DirectTransport;
using MediasoupSharp.Exceptions;
using MediasoupSharp.PipeTransport;
using MediasoupSharp.PlainTransport;
using MediasoupSharp.Producer;
using MediasoupSharp.RtpObserver;
using MediasoupSharp.RtpParameters;
using MediasoupSharp.SrtpParameters;
using MediasoupSharp.Transport;
using MediasoupSharp.WebRtcTransport;
using Microsoft.Extensions.Logging;

namespace MediasoupSharp.Router;

internal class Router<TRouterAppData> : Router
{
    internal Router(
        RouterInternal @internal,
        RouterData data,
        Channel.Channel channel,
        PayloadChannel.PayloadChannel payloadChannel,
        RouterInternal? appData = null)
        : base(
            @internal,
            data,
            channel,
            payloadChannel,
            appData)
    {
    }

    public new TRouterAppData AppData
    {
        get => (TRouterAppData)base.AppData;
        set => base.AppData = value!;
    }
}

internal class Router : EnhancedEventEmitter<RouterEvents>
{
    private readonly RouterInternal @internal;

    private readonly RouterData data;

    /// <summary>
    /// Channel instance.
    /// </summary>
    private readonly Channel.Channel channel;

    /// <summary>
    /// PayloadChannel instance.
    /// </summary>
    private readonly PayloadChannel.PayloadChannel payloadChannel;

    /// <summary>
    /// Whether the Router is closed.
    /// </summary>
    public bool Closed { get; private set; }

    // Custom app data.
    public object AppData { get; set; }

    /// <summary>
    /// Transports map.
    /// </summary>
    private readonly Dictionary<string, ITransport> transports = new();

    /// <summary>
    /// Producers map.
    /// </summary>
    private readonly Dictionary<string, Producer.Producer> producers = new();

    /// <summary>
    /// RtpObservers map.
    /// </summary>
    private readonly Dictionary<string, IRtpObserver> rtpObservers = new();

    /// <summary>
    /// DataProducers map.
    /// </summary>
    private readonly Dictionary<string, DataProducer.DataProducer> dataProducers = new();

    /// <summary>
    /// Map of PipeTransport pair Promises indexed by the id of the Router in
    /// which pipeToRouter() was called.
    /// </summary>
    private readonly Dictionary<string, Task<PipeTransportPair>> mapRouterPairPipeTransportPairPromise = new();

    /// <summary>
    /// Observer instance.
    /// </summary>
    public EnhancedEventEmitter<RouterObserverEvents> Observer => observer ??= new();

    private EnhancedEventEmitter<RouterObserverEvents>? observer;

    public override ILoggerFactory LoggerFactory
    {
        init
        {
            observer = new EnhancedEventEmitter<RouterObserverEvents>
            {
                LoggerFactory = value
            };
            base.LoggerFactory = value;
        }
    }

    internal Router(
        RouterInternal @internal,
        RouterData data,
        Channel.Channel channel,
        PayloadChannel.PayloadChannel payloadChannel,
        object? appData = null
    )
    {
        this.@internal      = @internal;
        this.data           = data;
        this.channel        = channel;
        this.payloadChannel = payloadChannel;
        AppData             = appData ?? new();
    }

    public string Id => @internal.RouterId;

    public RtpCapabilities RtpCapabilities => data.RtpCapabilities;

    public Dictionary<string, ITransport> TransportsForTesting => transports;


    /// <summary>
    /// Close the Router.
    /// </summary>
    public void Close()
    {
        if (Closed)
        {
            return;
        }

        Logger?.LogDebug("CloseAsync() | Router:{Id}", Id);

        Closed = true;

        var reqData = new { routerId = @internal.RouterId };

        channel.Request("worker.closeRouter", null, reqData)
            .ContinueWith(_ => { }, TaskContinuationOptions.OnlyOnFaulted);

        // Close every Transport.
        foreach (var transport in transports.Values)
        {
            transport.RouterClosed();
        }

        transports.Clear();

        // Clear the Producers map.
        producers.Clear();

        // Close every RtpObserver.
        foreach (var rtpObserver in rtpObservers.Values)
        {
            rtpObserver.RouterClosed();
        }

        rtpObservers.Clear();

        // Clear the DataProducers map.
        dataProducers.Clear();

        _ = Emit("@close");

        // Emit observer event.
        _ = Observer.SafeEmit("close");
    }

    /// <summary>
    /// Worker was closed.
    /// </summary>
    private void WorkerClosed()
    {
        if (Closed)
        {
            return;
        }

        Logger?.LogDebug("WorkerClosedAsync() | Router:{Id}", Id);

        Closed = true;

        // Close every Transport.
        foreach (var transport in transports.Values)
        {
            transport.RouterClosed();
        }

        transports.Clear();

        // Clear the Producers map.
        producers.Clear();

        // Close every RtpObserver.
        foreach (var rtpObserver in rtpObservers.Values)
        {
            rtpObserver.RouterClosed();
        }

        rtpObservers.Clear();

        // Clear the DataProducers map.
        dataProducers.Clear();

        _ = SafeEmit("workerclose");

        // Emit observer event.
        _ = Observer.SafeEmit("close");
    }

    /// <summary>
    /// Dump Router.
    /// </summary>
    public async Task<object> DumpAsync()
    {
        Logger?.LogDebug("DumpAsync() | Router:{Id}", Id);

        return (await channel.Request("router.dump", @internal.RouterId))!;
    }

    /// <summary>
    /// Create a WebRtcTransport.
    /// </summary>
    public async Task<WebRtcTransport<TWebRtcTransportAppData>> CreateWebRtcTransportAsync<TWebRtcTransportAppData>(
        WebRtcTransportOptions<TWebRtcTransportAppData> webRtcTransportOptions)
    {
        var webRtcServer                    = webRtcTransportOptions.WebRtcServer;
        var port                            = webRtcTransportOptions.Port;
        var enableUdp                       = webRtcTransportOptions.EnableUdp ?? true;
        var enableTcp                       = webRtcTransportOptions.EnableTcp ?? false;
        var preferUdp                       = webRtcTransportOptions.PreferUdp ?? false;
        var preferTcp                       = webRtcTransportOptions.PreferTcp ?? false;
        var initialAvailableOutgoingBitrate = webRtcTransportOptions.InitialAvailableOutgoingBitrate ?? 600000;
        var enableSctp                      = webRtcTransportOptions.EnableSctp ?? false;
        var numSctpStreams                  = webRtcTransportOptions.NumSctpStreams ?? new() { OS = 1024, MIS = 1024 };
        var maxSctpMessageSize              = webRtcTransportOptions.MaxSctpMessageSize ?? 262144;
        var sctpSendBufferSize              = webRtcTransportOptions.SctpSendBufferSize ?? 262144;
        var appData                         = webRtcTransportOptions.AppData;

        Logger?.LogDebug("CreateWebRtcTransportAsync()");

        if (webRtcServer == null && webRtcTransportOptions.ListenIps.IsNullOrEmpty())
        {
            throw new TypeError("missing webRtcServer and listenIps (one of them is mandatory)");
        }

        List<TransportListenIp>? listenIps = null;
        if (webRtcTransportOptions.ListenIps != null)
        {
            listenIps = webRtcTransportOptions.ListenIps.Select(listenIp =>
            {
                return listenIp switch
                {
                    string str           => new TransportListenIp { Ip = str },
                    TransportListenIp ip => new TransportListenIp { Ip = ip.Ip, AnnouncedIp = ip.AnnouncedIp },
                    _                    => throw new TypeError("wrong listenIp")
                };
            }).ToList();
        }

        if (webRtcServer != null)
        {
            webRtcTransportOptions.ListenIps = null;
            webRtcTransportOptions.Port      = null;
        }

        // TODO : Naming
        var reqData = new
        {
            TransportId = Guid.NewGuid().ToString(),
            webRtcServer?.WebRtcServerId,
            ListenIps                       = listenIps,
            Port                            = port,
            EnableUdp                       = enableUdp,
            EnableTcp                       = enableTcp,
            PreferUdp                       = preferUdp,
            PreferTcp                       = preferTcp,
            InitialAvailableOutgoingBitrate = initialAvailableOutgoingBitrate,
            EnableSctp                      = enableSctp,
            NumSctpStreams                  = numSctpStreams,
            MaxSctpMessageSize              = maxSctpMessageSize,
            SctpSendBufferSize              = sctpSendBufferSize,
            IsDataChannel                   = true
        };

        var data = (WebRtcTransportData)(await channel.Request(
            webRtcTransportOptions.WebRtcServer != null
                ? "router.createWebRtcTransportWithServer"
                : "router.createWebRtcTransport", @internal.RouterId, reqData))!;

        var transport = new WebRtcTransport<TWebRtcTransportAppData>(new()
            {
                Internal = new TransportInternal
                {
                    RouterId    = @internal.RouterId,
                    TransportId = reqData.TransportId
                },
                Data = data, // 直接使用返回值
                Channel = channel,
                PayloadChannel = payloadChannel,
                AppData = webRtcTransportOptions.AppData,
                GetRouterRtpCapabilities = () => this.data.RtpCapabilities,
                GetProducerById = producerId => producers.TryGetValue(producerId, out var producer) ? producer : null,
                GetDataProducerById = producerId =>
                    dataProducers.TryGetValue(producerId, out var producer) ? producer : null
            }
        );

        transports[transport.Id] = transport;
        transport.On("@close", async _ => { transports.Remove(transport.Id); });
        transport.On("@listenserverclose", async _ => { transports.Remove(transport.Id); });
        transport.On("@newproducer", async args =>
        {
            var producer = (Producer.Producer)args![1];
            producers[producer.Id] = producer;
        });
        transport.On("@producerclose", async args =>
        {
            var producer = (Producer.Producer)args![1];
            producers.Remove(producer.Id);
        });
        transport.On("@newdataproducer", async args =>
        {
            var dataProducer = (DataProducer.DataProducer)args![1];
            dataProducers[dataProducer.Id] = dataProducer;
        });
        transport.On("@dataproducerclose", async args =>
        {
            var dataProducer = (DataProducer.DataProducer)args![1];
            dataProducers.Remove(dataProducer.Id);
        });

        // Emit observer event.
        await Observer.SafeEmit("newtransport", transport);

        webRtcServer?.HandleWebRtcTransport(transport);

        return transport;
    }

    /// <summary>
    /// Create a PlainTransport.
    /// </summary>
    public async Task<PlainTransport<TPlainTransportAppData>> CreatePlainTransportAsync<TPlainTransportAppData>(
        PlainTransportOptions<TPlainTransportAppData> plainTransportOptions)
    {
        var listenIp           = plainTransportOptions.ListenIp;
        var port               = plainTransportOptions.Port;
        var rtcpMux            = plainTransportOptions.RtcpMux            ?? true;
        var comedia            = plainTransportOptions.Comedia            ?? false;
        var enableSctp         = plainTransportOptions.EnableSctp         ?? false;
        var numSctpStreams     = plainTransportOptions.NumSctpStreams     ?? new() { OS = 1024, MIS = 1024 };
        var maxSctpMessageSize = plainTransportOptions.MaxSctpMessageSize ?? 262144;
        var sctpSendBufferSize = plainTransportOptions.SctpSendBufferSize ?? 262144;
        var enableSrtp         = plainTransportOptions.EnableSrtp         ?? false;
        var srtpCryptoSuite    = plainTransportOptions.SrtpCryptoSuite    ?? SrtpCryptoSuite.AES_CM_128_HMAC_SHA1_80;
        var appData            = plainTransportOptions.AppData;

        Logger?.LogDebug("CreatePlainTransportAsync()");

        if (listenIp == null)
        {
            throw new TypeError("missing listenIp");
        }

        listenIp = listenIp switch
        {
            string str           => new TransportListenIp { Ip = str },
            TransportListenIp ip => ip with { },
            _                    => throw new TypeError("wrong listenIp"),
        };

        var reqData = new
        {
            TransportId        = Guid.NewGuid().ToString(),
            ListenIp           = listenIp,
            Port               = port,
            RtcpMux            = rtcpMux,
            Comedia            = comedia,
            EnableSctp         = enableSctp,
            NumSctpStreams     = numSctpStreams,
            MaxSctpMessageSize = maxSctpMessageSize,
            SctpSendBufferSize = sctpSendBufferSize,
            IsDataChannel      = false,
            EnableSrtp         = enableSrtp,
            SrtpCryptoSuite    = srtpCryptoSuite
        };

        var data =
            await channel.Request("router.createPlainTransport", @internal.RouterId, reqData) as PlainTransportData;

        var transport = new PlainTransport<TPlainTransportAppData>(new()
        {
            Internal = new TransportInternal
            {
                RouterId    = @internal.RouterId,
                TransportId = reqData.TransportId
            },
            Data = data, // 直接使用返回值
            Channel = channel,
            PayloadChannel = payloadChannel,
            AppData = appData,
            GetRouterRtpCapabilities = () => this.data.RtpCapabilities,
            GetProducerById = producerId => producers.TryGetValue(producerId, out var producer) ? producer : null,
            GetDataProducerById = producerId =>
                dataProducers.TryGetValue(producerId, out var producer) ? producer : null
        });
        
        transports[transport.Id] = transport;
        transport.On("@close", async _ => { transports.Remove(transport.Id); });
        transport.On("@listenserverclose", async _ => { transports.Remove(transport.Id); });
        transport.On("@newproducer", async args =>
        {
            var producer = (Producer.Producer)args![1];
            producers[producer.Id] = producer;
        });
        transport.On("@producerclose", async args =>
        {
            var producer = (Producer.Producer)args![1];
            producers.Remove(producer.Id);
        });
        transport.On("@newdataproducer", async args =>
        {
            var dataProducer = (DataProducer.DataProducer)args![1];
            dataProducers[dataProducer.Id] = dataProducer;
        });
        transport.On("@dataproducerclose", async args =>
        {
            var dataProducer = (DataProducer.DataProducer)args![1];
            dataProducers.Remove(dataProducer.Id);
        });

        // Emit observer event.
        await Observer.SafeEmit("newtransport", transport);
        
        return transport;
    }

    /// <summary>
    /// Create a PipeTransport.
    /// </summary>
    public async Task<PipeTransport<TPipeTransportAppData>> CreatePipeTransportAsync<TPipeTransportAppData>(
        PipeTransportOptions<TPipeTransportAppData> pipeTransportOptions)
    {
        var listenIp           = pipeTransportOptions.ListenIp;
        var port               = pipeTransportOptions.Port;
        var enableSctp         = pipeTransportOptions.EnableSctp         ?? false;
        var numSctpStreams     = pipeTransportOptions.NumSctpStreams     ?? new() { OS = 1024, MIS = 1024 };
        var maxSctpMessageSize = pipeTransportOptions.MaxSctpMessageSize ?? 268435456;
        var sctpSendBufferSize = pipeTransportOptions.SctpSendBufferSize ?? 268435456;
        var enableRtx          = pipeTransportOptions.EnableRtx          ?? false;
        var enableSrtp         = pipeTransportOptions.EnableSrtp         ?? false;
        var appData            = pipeTransportOptions.AppData;
        
        Logger?.LogDebug("CreatePipeTransportAsync()");

        if (listenIp == null)
        {
            throw new TypeError("missing listenIp");
        }
        
        listenIp = listenIp switch
        {
            string str           => new TransportListenIp { Ip = str },
            TransportListenIp ip => ip with { },
            _                    => throw new TypeError("wrong listenIp"),
        };

        var reqData = new
        {
            TransportId        = Guid.NewGuid().ToString(),
            ListenIp           = listenIp,
            Port               = port,
            EnableSctp         = enableSctp,
            NumSctpStreams     = numSctpStreams,
            MaxSctpMessageSize = maxSctpMessageSize,
            SctpSendBufferSize = sctpSendBufferSize,
            IsDataChannel      = false,
            EnableRtx          = enableRtx,
            EnableSrtp         = enableSrtp
        };

        var data =
            await channel.Request("router.createPipeTransport", @internal.RouterId, reqData) as PipeTransportData;

        var transport =  new PipeTransport<TPipeTransportAppData>(new()
        {
            Internal = new TransportInternal
            {
                RouterId    = @internal.RouterId,
                TransportId = reqData.TransportId
            },
            Data = data, // 直接使用返回值
            Channel = channel,
            PayloadChannel = payloadChannel,
            AppData = appData,
            GetRouterRtpCapabilities = () => this.data.RtpCapabilities,
            GetProducerById = producerId => producers.TryGetValue(producerId, out var producer) ? producer : null,
            GetDataProducerById = producerId =>
                dataProducers.TryGetValue(producerId, out var producer) ? producer : null
        });

        // Emit observer event.
        await Observer.SafeEmit("newtransport", transport);
        return transport;
    }

    /// <summary>
    /// Create a DirectTransport.
    /// </summary>
    /// <param name="directTransportOptions"></param>
    /// <returns></returns>
    public async Task<DirectTransport.DirectTransport> CreateDirectTransportAsync(
        DirectTransportOptions directTransportOptions)
    {
        Logger?.LogDebug("CreateDirectTransportAsync()");

        using (await closeLock.ReadLockAsync())
        {
            if (Closed)
            {
                throw new InvalidStateException("Router closed");
            }

            var reqData = new
            {
                TransportId = Guid.NewGuid().ToString(),
                Direct      = true,
                directTransportOptions.MaxMessageSize,
            };

            var resData =
                await channel.RequestAsync(MethodId.ROUTER_CREATE_DIRECT_TRANSPORT, @internal.RouterId, reqData);
            var responseData = resData!.Deserialize<RouterCreateDirectTransportResponseData>();

            var transport = new DirectTransport.DirectTransport(Logger ? Factory,
                new TransportInternal(RouterId, reqData.TransportId),
                responseData, // 直接使用返回值
                channel,
                payloadChannel,
                directTransportOptions.AppData,
                () => data.RtpCapabilities,
                async m =>
                {
                    using (await producersLock.ReadLockAsync())
                    {
                        return producers.TryGetValue(m, out var p) ? p : null;
                    }
                },
                async m =>
                {
                    using (await dataProducersLock.ReadLockAsync())
                    {
                        return dataProducers.TryGetValue(m, out var p) ? p : null;
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
        if (webRtcServer != null && transport is WebRtcTransport.WebRtcTransport webRtcTransport)
        {
            await webRtcServer.HandleWebRtcTransportAsync(webRtcTransport);
        }

        // Emit observer event.
        Observer.Emit("newtransport", transport);
    }

    /// <summary>
    /// Pipes the given Producer or DataProducer into another Router in same host.
    /// </summary>
    /// <param name="pipeToRouterOptions">ListenIp 传入 127.0.0.1, EnableSrtp 传入 true 。</param>
    /// <returns></returns>
    public async Task<PipeToRouterResult> PipeToRouterAsync(PipeToRouterOptions pipeToRouterOptions)
    {
        using (await closeLock.ReadLockAsync())
        {
            if (Closed)
            {
                throw new InvalidStateException("Router closed");
            }

            if (pipeToRouterOptions.ListenIp == null)
            {
                throw new ArgumentNullException(nameof(pipeToRouterOptions.ListenIp), "Missing listenIp");
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
                throw new ArgumentNullException(nameof(pipeToRouterOptions.Router), "Router not found");
            }

            if (pipeToRouterOptions.Router == this)
            {
                throw new ArgumentException("Cannot use this Router as destination");
            }

            Producer.Producer?         producer     = null;
            DataProducer.DataProducer? dataProducer = null;

            if (!pipeToRouterOptions.ProducerId.IsNullOrWhiteSpace())
            {
                using (await producersLock.ReadLockAsync())
                {
                    if (!producers.TryGetValue(pipeToRouterOptions.ProducerId!, out producer))
                    {
                        throw new Exception("Producer not found");
                    }
                }
            }
            else if (!pipeToRouterOptions.DataProducerId.IsNullOrWhiteSpace())
            {
                using (await dataProducersLock.ReadLockAsync())
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
            using (await mapRouterPipeTransportsLock.WriteLockAsync())
            {
                if (mapRouterPairPipeTransportPairPromise.TryGetValue(pipeToRouterOptions.Router,
                        out var pipeTransportPair))
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
                                ListenIp       = pipeToRouterOptions.ListenIp,
                                EnableSctp     = pipeToRouterOptions.EnableSctp,
                                NumSctpStreams = pipeToRouterOptions.NumSctpStreams,
                                EnableRtx      = pipeToRouterOptions.EnableRtx,
                                EnableSrtp     = pipeToRouterOptions.EnableSrtp
                            }),
                            pipeToRouterOptions.Router.CreatePipeTransportAsync(new PipeTransportOptions
                            {
                                ListenIp       = pipeToRouterOptions.ListenIp,
                                EnableSctp     = pipeToRouterOptions.EnableSctp,
                                NumSctpStreams = pipeToRouterOptions.NumSctpStreams,
                                EnableRtx      = pipeToRouterOptions.EnableRtx,
                                EnableSrtp     = pipeToRouterOptions.EnableSrtp
                            })
                        );

                        localPipeTransport  = pipeTransports[0];
                        remotePipeTransport = pipeTransports[1];

                        await Task.WhenAll(localPipeTransport.ConnectAsync(new PipeTransportConnectParameters
                            {
                                Ip             = remotePipeTransport.data.Tuple.LocalIp,
                                Port           = remotePipeTransport.data.Tuple.LocalPort,
                                SrtpParameters = remotePipeTransport.data.SrtpParameters,
                            }),
                            remotePipeTransport.ConnectAsync(new PipeTransportConnectParameters
                            {
                                Ip             = localPipeTransport.data.Tuple.LocalIp,
                                Port           = localPipeTransport.data.Tuple.LocalPort,
                                SrtpParameters = localPipeTransport.data.SrtpParameters,
                            })
                        );

                        localPipeTransport.Observer.On("close", async (_, _) =>
                        {
                            await remotePipeTransport.CloseAsync();
                            using (await mapRouterPipeTransportsLock.WriteLockAsync())
                            {
                                mapRouterPairPipeTransportPairPromise.Remove(pipeToRouterOptions.Router);
                            }
                        });

                        remotePipeTransport.Observer.On("close", async (_, _) =>
                        {
                            await localPipeTransport.CloseAsync();
                            using (await mapRouterPipeTransportsLock.WriteLockAsync())
                            {
                                mapRouterPairPipeTransportPairPromise.Remove(pipeToRouterOptions.Router);
                            }
                        });

                        mapRouterPairPipeTransportPairPromise[pipeToRouterOptions.Router] =
                            new[] { localPipeTransport, remotePipeTransport };
                    }
                    catch (Exception ex)
                    {
                        Logger?.LogError(ex, $"PipeToRouterAsync() | Create PipeTransport pair failed.");

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
                    pipeConsumer.Observer.On("close", async (_, _) => await pipeProducer.CloseAsync());
                    pipeConsumer.Observer.On("pause", async (_, _) => await pipeProducer.PauseAsync());
                    pipeConsumer.Observer.On("resume", async (_, _) => await pipeProducer.ResumeAsync());

                    // Pipe events from the pipe Producer to the pipe Consumer.
                    pipeProducer.Observer.On("close", async (_, _) => await pipeConsumer.CloseAsync());

                    return new PipeToRouterResult { PipeConsumer = pipeConsumer, PipeProducer = pipeProducer };
                }
                catch (Exception ex)
                {
                    Logger?.LogError(ex, $"PipeToRouterAsync() | Create pipe Consumer/Producer pair failed");

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
            else if (dataProducer != null)
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
                        Id                   = dataProducer.DataProducerId,
                        SctpStreamParameters = pipeDataConsumer.Data.SctpStreamParameters,
                        Label                = pipeDataConsumer.Data.Label,
                        Protocol             = pipeDataConsumer.Data.Protocol,
                        AppData              = dataProducer.AppData,
                    });

                    // Pipe events from the pipe DataConsumer to the pipe DataProducer.
                    pipeDataConsumer.Observer.On("close", async (_, _) => await pipeDataProducer.CloseAsync());

                    // Pipe events from the pipe DataProducer to the pipe DataConsumer.
                    pipeDataProducer.observer.On("close", async (_, _) => await pipeDataConsumer.CloseAsync());

                    return new PipeToRouterResult
                        { PipeDataConsumer = pipeDataConsumer, PipeDataProducer = pipeDataProducer };
                }
                catch (Exception ex)
                {
                    Logger?.LogError(ex, $"PipeToRouterAsync() | Create pipe DataConsumer/DataProducer pair failed.");

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
            else
            {
                throw new Exception("Internal error");
            }
        }
    }

    /// <summary>
    /// Create an ActiveSpeakerObserver
    /// </summary>
    public async Task<ActiveSpeakerObserver.ActiveSpeakerObserver> CreateActiveSpeakerObserverAsync(
        ActiveSpeakerObserverOptions activeSpeakerObserverOptions)
    {
        Logger?.LogDebug("CreateActiveSpeakerObserverAsync()");

        using (await closeLock.ReadLockAsync())
        {
            if (Closed)
            {
                throw new InvalidStateException("Router closed");
            }

            var reqData = new
            {
                RtpObserverId = Guid.NewGuid().ToString(),
                activeSpeakerObserverOptions.Interval
            };

            // Fire and forget
            channel.RequestAsync(MethodId.ROUTER_CREATE_ACTIVE_SPEAKER_OBSERVER, @internal.RouterId, reqData)
                .ContinueWithOnFaultedHandleLog(Logger ?);

            var activeSpeakerObserver = new ActiveSpeakerObserver.ActiveSpeakerObserver(Logger ? Factory,
                new RtpObserverInternal(@internal.RouterId, reqData.RtpObserverId),
                channel,
                payloadChannel,
                activeSpeakerObserverOptions.AppData,
                async m =>
                {
                    using (await producersLock.ReadLockAsync())
                    {
                        return producers.TryGetValue(m, out var p) ? p : null;
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
        Logger?.LogDebug("CreateAudioLevelObserverAsync()");

        using (await closeLock.ReadLockAsync())
        {
            if (Closed)
            {
                throw new InvalidStateException("Router closed");
            }

            var reqData = new
            {
                RtpObserverId = Guid.NewGuid().ToString(),
                audioLevelObserverOptions.MaxEntries,
                audioLevelObserverOptions.Threshold,
                audioLevelObserverOptions.Interval
            };

            // Fire and forget
            channel.RequestAsync(MethodId.ROUTER_CREATE_AUDIO_LEVEL_OBSERVER, @internal.RouterId, reqData)
                .ContinueWithOnFaultedHandleLog(Logger ?);

            var audioLevelObserver = new AudioLevelObserver.AudioLevelObserver(Logger ? Factory,
                new RtpObserverInternal(@internal.RouterId, reqData.RtpObserverId),
                channel,
                payloadChannel,
                audioLevelObserverOptions.AppData,
                async m =>
                {
                    using (await producersLock.ReadLockAsync())
                    {
                        return producers.TryGetValue(m, out var p) ? p : null;
                    }
                });
            await ConfigureRtpObserverAsync(audioLevelObserver);

            return audioLevelObserver;
        }
    }

    private Task ConfigureRtpObserverAsync(RtpObserver.RtpObserver rtpObserver)
    {
        rtpObservers[rtpObserver.Internal.RtpObserverId] = rtpObserver;
        rtpObserver.On("@close", async (_, _) =>
        {
            using (await rtpObserversLock.WriteLockAsync())
            {
                rtpObservers.Remove(rtpObserver.Internal.RtpObserverId);
            }
        });

        // Emit observer event.
        Observer.Emit("newrtpobserver", rtpObserver);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Check whether the given RTP capabilities can consume the given Producer.
    /// </summary>
    public async Task<bool> CanConsumeAsync(string producerId, RtpCapabilities rtpCapabilities)
    {
        using (await producersLock.ReadLockAsync())
        {
            if (!producers.TryGetValue(producerId, out var producer))
            {
                Logger?.LogError($"CanConsume() | Producer with id {producerId} not found");
                return false;
            }

            try
            {
                return ORTC.Ortc.CanConsume(producer.Data.ConsumableRtpParameters, rtpCapabilities);
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "CanConsume() | Unexpected error");
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

        return RouterId == other.RouterId;
    }

    public override bool Equals(object? other)
    {
        return Equals(other as Router);
    }

    public override int GetHashCode()
    {
        return RouterId.GetHashCode();
    }

    #endregion IEquatable<T>
}