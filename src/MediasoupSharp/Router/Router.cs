using System.Globalization;
using MediasoupSharp.ActiveSpeakerObserver;
using MediasoupSharp.AudioLevelObserver;
using MediasoupSharp.Channel;
using MediasoupSharp.Consumer;
using MediasoupSharp.DataConsumer;
using MediasoupSharp.DataProducer;
using MediasoupSharp.DirectTransport;
using MediasoupSharp.Exceptions;
using MediasoupSharp.ORTC;
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
        TRouterAppData? appData,
        ILoggerFactory? loggerFactory = null)
        : base(
            @internal,
            data,
            channel,
            payloadChannel,
            appData,
            loggerFactory)
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
    private readonly ILogger? logger;
    
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
    public EnhancedEventEmitter<RouterObserverEvents> Observer { get; }

    internal Router(
        RouterInternal @internal,
        RouterData data,
        Channel.Channel channel,
        PayloadChannel.PayloadChannel payloadChannel,
        object? appData = null,
        ILoggerFactory? loggerFactory = null
    ) : base(loggerFactory)
    {
        logger              = loggerFactory?.CreateLogger(GetType());
        this.@internal      = @internal;
        this.data           = data;
        this.channel        = channel;
        this.payloadChannel = payloadChannel;
        AppData             = appData ?? new();
        Observer            = new(loggerFactory);

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

        logger?.LogDebug("CloseAsync() | Router:{Id}", Id);

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
    public void WorkerClosed()
    {
        if (Closed)
        {
            return;
        }

        logger?.LogDebug("WorkerClosedAsync() | Router:{Id}", Id);

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
        logger?.LogDebug("DumpAsync() | Router:{Id}", Id);

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

        logger?.LogDebug("CreateWebRtcTransportAsync()");

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
            TransportId                     = Guid.NewGuid().ToString(),
            WebRtcServerId                  = webRtcServer?.Id,
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

        logger?.LogDebug("CreatePlainTransportAsync()");

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

        // TODO : Naming
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

        logger?.LogDebug("CreatePipeTransportAsync()");

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

        // TODO : Naming
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

        var transport = new PipeTransport<TPipeTransportAppData>(new()
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
    /// Create a DirectTransport.
    /// </summary>
    /// <param name="directTransportOptions"></param>
    /// <returns></returns>
    public async Task<DirectTransport<TDirectTransportAppData>> CreateDirectTransportAsync<TDirectTransportAppData>(
        DirectTransportOptions<TDirectTransportAppData> directTransportOptions)
    {
        var maxMessageSize = directTransportOptions.MaxMessageSize;
        var appData        = directTransportOptions.AppData;
        logger?.LogDebug("CreateDirectTransportAsync()");

        var reqData = new
        {
            TransportId = Guid.NewGuid().ToString(),
            Direct      = true,
            directTransportOptions.MaxMessageSize,
        };

        var data =
            await channel.Request("router.createDirectTransport", @internal.RouterId, reqData) as DirectTransportData;

        var transport = new DirectTransport<TDirectTransportAppData>(
            new DirectTransportConstructorOptions<TDirectTransportAppData>()
            {
                Internal = new TransportInternal
                {
                    RouterId    = @internal.RouterId,
                    TransportId = reqData.TransportId
                },
                Data                     = data, // 直接使用返回值
                Channel                  = channel,
                PayloadChannel           = payloadChannel,
                AppData                  = directTransportOptions.AppData,
                GetRouterRtpCapabilities = () => this.data.RtpCapabilities,
                GetProducerById          = producerId => producers.TryGetValue(producerId, out var p) ? p : null,
                GetDataProducerById      = producerId => dataProducers.TryGetValue(producerId, out var p) ? p : null
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
    /// Pipes the given Producer or DataProducer into another Router in same host.
    /// </summary>
    /// <param name="pipeToRouterOptions">ListenIp 传入 127.0.0.1, EnableSrtp 传入 true 。</param>
    /// <returns></returns>
    public async Task<PipeToRouterResult> PipeToRouterAsync(PipeToRouterOptions pipeToRouterOptions)
    {
        var producerId     = pipeToRouterOptions.ProducerId;
        var dataProducerId = pipeToRouterOptions.DataProducerId;
        var router         = pipeToRouterOptions.Router;
        var listenIp       = pipeToRouterOptions.ListenIp       ?? "127.0.0.1";
        var enableSctp     = pipeToRouterOptions.EnableSctp     ?? true;
        var numSctpStreams = pipeToRouterOptions.NumSctpStreams ?? new() { OS = 1024, MIS = 1024 };
        var enableRtx      = pipeToRouterOptions.EnableRtx      ?? false;
        var enableSrtp     = pipeToRouterOptions.EnableSrtp     ?? false;

        if (producerId.IsNullOrEmpty() && dataProducerId.IsNullOrEmpty())
        {
            throw new TypeError("Missing producerId or dataProducerId");
        }
        else if (!producerId.IsNullOrEmpty() && !dataProducerId.IsNullOrEmpty())
        {
            throw new TypeError("just producerId or dataProducerId can be given");
        }
        else if (router == null)
        {
            throw new TypeError("Router not found");
        }
        else if (router.Equals(this))
        {
            throw new TypeError("Cannot use this Router as destination");
        }


        Producer.Producer?         producer     = null;
        DataProducer.DataProducer? dataProducer = null;

        if (!producerId.IsNullOrEmpty())
        {
            if (!producers.TryGetValue(pipeToRouterOptions.ProducerId!, out producer))
            {
                throw new TypeError("Producer not found");
            }
        }
        else if (!dataProducerId.IsNullOrEmpty())
        {
            if (!dataProducers.TryGetValue(pipeToRouterOptions.DataProducerId!, out dataProducer))
            {
                throw new TypeError("DataProducer not found");
            }
        }

        var pipeTransportPairKey = router.Id;
        var pipeTransportPairPromise =
            mapRouterPairPipeTransportPairPromise.GetValueOrDefault(pipeTransportPairKey);
        PipeTransportPair?           pipeTransportPair;
        PipeTransport.PipeTransport? localPipeTransport  = null;
        PipeTransport.PipeTransport? remotePipeTransport = null;

        // 因为有可能新增，所以用写锁。
        if (pipeTransportPairPromise != null)
        {
            pipeTransportPair   = await pipeTransportPairPromise;
            localPipeTransport  = pipeTransportPair[Id];
            remotePipeTransport = pipeTransportPair[router.Id];
        }
        else
        {
            var promise = new TaskCompletionSource<PipeTransportPair>();
            pipeTransportPairPromise = promise.Task;

            _ = Task.Run(async () =>
            {
                try
                {
                    var t1 = CreatePipeTransportAsync(
                        new PipeTransportOptions<IDictionary<string, object?>>
                        {
                            ListenIp       = listenIp,
                            EnableSctp     = enableSctp,
                            NumSctpStreams = numSctpStreams,
                            EnableRtx      = enableRtx,
                            EnableSrtp     = enableSrtp
                        });
                    var t2 = router.CreatePipeTransportAsync(
                        new PipeTransportOptions<IDictionary<string, object?>>
                        {
                            ListenIp       = listenIp,
                            EnableSctp     = enableSctp,
                            NumSctpStreams = numSctpStreams,
                            EnableRtx      = enableRtx,
                            EnableSrtp     = enableSrtp
                        });
                    localPipeTransport  = (PipeTransport.PipeTransport)await t1;
                    remotePipeTransport = (PipeTransport.PipeTransport)await t2;
                    var t3 = localPipeTransport.ConnectAsync(new
                    {
                        Ip             = remotePipeTransport.Tuple.LocalIp,
                        Port           = remotePipeTransport.Tuple.LocalPort,
                        SrtpParameters = remotePipeTransport.SrtpParameters,
                    });
                    var t4 = remotePipeTransport.ConnectAsync(new
                    {
                        Ip             = localPipeTransport.Tuple.LocalIp,
                        Port           = localPipeTransport.Tuple.LocalPort,
                        SrtpParameters = localPipeTransport.SrtpParameters,
                    });
                    await t3;
                    await t4;
                    localPipeTransport!.Observer.On("close", async _ =>
                    {
                        remotePipeTransport!.Close();
                        mapRouterPairPipeTransportPairPromise.Remove(
                            pipeTransportPairKey);
                    });

                    remotePipeTransport!.Observer.On("close", async _ =>
                    {
                        localPipeTransport.Close();
                        mapRouterPairPipeTransportPairPromise.Remove(
                            pipeTransportPairKey);
                    });

                    promise.SetResult(new PipeTransportPair
                    {
                        { Id, localPipeTransport },
                        { router.Id, remotePipeTransport }
                    });
                }
                catch (Exception exception)
                {
                    logger?.LogError("pipeToRouter() | error creating PipeTransport pair:{E}", exception);
                    localPipeTransport?.Close();
                    remotePipeTransport?.Close();
                    promise.SetException(exception);
                }
            });

            mapRouterPairPipeTransportPairPromise[pipeTransportPairKey] = pipeTransportPairPromise;

            router.AddPipeTransportPair(Id, pipeTransportPairPromise);

            await pipeTransportPairPromise;
        }

        if (producer != null)
        {
            Consumer.Consumer? pipeConsumer = null;
            Producer.Producer? pipeProducer = null;

            try
            {
                pipeConsumer = await localPipeTransport!.ConsumeAsync(new ConsumerOptions<object>
                {
                    ProducerId = pipeToRouterOptions.ProducerId!
                });

                pipeProducer = await remotePipeTransport!.ProduceAsync(new ProducerOptions<object>
                {
                    Id            = producer.Id,
                    Kind          = pipeConsumer.Kind,
                    RtpParameters = pipeConsumer.RtpParameters,
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
                pipeConsumer.Observer.On("close", async _ => pipeProducer.Close());
                pipeConsumer.Observer.On("pause", async _ => await pipeProducer.PauseAsync());
                pipeConsumer.Observer.On("resume", async _ => await pipeProducer.ResumeAsync());

                // Pipe events from the pipe Producer to the pipe Consumer.
                pipeProducer.Observer.On("close", async (_) => pipeConsumer.Close());

                return new PipeToRouterResult { PipeConsumer = pipeConsumer, PipeProducer = pipeProducer };
            }
            catch (Exception ex)
            {
                logger?.LogError("PipeToRouterAsync() | Create pipe Consumer/Producer pair failed:{E}", ex);

                pipeConsumer?.Close();

                pipeProducer?.Close();

                throw;
            }
        }
        else if (dataProducer != null)
        {
            DataConsumer.DataConsumer? pipeDataConsumer = null;
            DataProducer.DataProducer? pipeDataProducer = null;

            try
            {
                pipeDataConsumer = await localPipeTransport!.ConsumeDataAsync(
                    new DataConsumerOptions<object>
                    {
                        DataProducerId = pipeToRouterOptions.DataProducerId!
                    });

                pipeDataProducer = await remotePipeTransport!.ProduceDataAsync(
                    new DataProducerOptions<object>
                    {
                        Id                   = dataProducer.Id,
                        SctpStreamParameters = pipeDataConsumer.SctpStreamParameters,
                        Label                = pipeDataConsumer.Label,
                        Protocol             = pipeDataConsumer.Protocol,
                        AppData              = dataProducer.AppData,
                    });

                // Pipe events from the pipe DataConsumer to the pipe DataProducer.
                pipeDataConsumer.Observer.On("close", async _ => pipeDataProducer.Close());

                // Pipe events from the pipe DataProducer to the pipe DataConsumer.
                pipeDataProducer.Observer.On("close", async _ => pipeDataConsumer.Close());

                return new PipeToRouterResult
                    { PipeDataConsumer = pipeDataConsumer, PipeDataProducer = pipeDataProducer };
            }
            catch (Exception ex)
            {
                logger?.LogError("PipeToRouterAsync() | Create pipe DataConsumer/DataProducer pair {E}", ex);

                pipeDataConsumer?.Close();

                pipeDataProducer?.Close();

                throw;
            }
        }
        else
        {
            throw new Exception("Internal error");
        }
    }

    public void AddPipeTransportPair(string pipeTransportPairKey, Task<PipeTransportPair> pipeTransportPairPromise)
    {
        if (mapRouterPairPipeTransportPairPromise.ContainsKey(pipeTransportPairKey))
        {
            throw new Exception(
                "given pipeTransportPairKey already exists in this Router");
        }

        mapRouterPairPipeTransportPairPromise[pipeTransportPairKey] = pipeTransportPairPromise;

        pipeTransportPairPromise
            .ContinueWith((pipeTransportPairTask) =>
            {
                var pipeTransportPair  = pipeTransportPairTask.Result;
                var localPipeTransport = pipeTransportPair[Id];

                // NOTE: No need to do any other cleanup here since that is done by the
                // Router calling this method on us.
                localPipeTransport.Observer.On("close", async _ =>
                {
                    mapRouterPairPipeTransportPairPromise.Remove(
                        pipeTransportPairKey);
                });
            }, TaskContinuationOptions.OnlyOnRanToCompletion);
        pipeTransportPairPromise
            .ContinueWith(_ =>
            {
                mapRouterPairPipeTransportPairPromise.Remove(
                    pipeTransportPairKey);
            }, TaskContinuationOptions.OnlyOnFaulted);
    }

    /// <summary>
    /// Create an ActiveSpeakerObserver
    /// </summary>
    public async Task<ActiveSpeakerObserver<TActiveSpeakerObserverAppData>> CreateActiveSpeakerObserverAsync<
        TActiveSpeakerObserverAppData>(
        ActiveSpeakerObserverOptions<TActiveSpeakerObserverAppData> activeSpeakerObserverOptions)
    {
        var interval = activeSpeakerObserverOptions.Interval ?? 300;
        var appData  = activeSpeakerObserverOptions.AppData;

        logger?.LogDebug("CreateActiveSpeakerObserverAsync()");

        var reqData = new
        {
            RtpObserverId = Guid.NewGuid().ToString(),
            activeSpeakerObserverOptions.Interval
        };

        // Fire and forget
        await channel.Request("router.createActiveSpeakerObserver", @internal.RouterId, reqData);

        var activeSpeakerObserver = new ActiveSpeakerObserver<TActiveSpeakerObserverAppData>(new()
        {
            Internal = new RtpObserverObserverInternal
            {
                RouterId      = @internal.RouterId,
                RtpObserverId = reqData.RtpObserverId
            },
            Channel         = channel,
            PayloadChannel  = payloadChannel,
            AppData         = appData,
            GetProducerById = producerId => producers.GetValueOrDefault(producerId),
        });

        rtpObservers[activeSpeakerObserver.Id] = activeSpeakerObserver;
        activeSpeakerObserver.On("@close", async _ => { rtpObservers.Remove(activeSpeakerObserver.Id); });

        // Emit observer event.
        await Observer.SafeEmit("newrtpobserver", activeSpeakerObserver);

        return activeSpeakerObserver;
    }

    /// <summary>
    /// Create an AudioLevelObserver.
    /// </summary>
    public async Task<AudioLevelObserver<TAudioLevelObserverAppData>> CreateAudioLevelObserverAsync<
        TAudioLevelObserverAppData>(
        AudioLevelObserverOptions<TAudioLevelObserverAppData> audioLevelObserverOptions)
    {
        var maxEntries = audioLevelObserverOptions.MaxEntries ?? 1;
        var threshold  = audioLevelObserverOptions.Threshold  ?? -80;
        var interval   = audioLevelObserverOptions.Interval   ?? 1000;
        var appData    = audioLevelObserverOptions.AppData;

        logger?.LogDebug("CreateAudioLevelObserverAsync()");

        // TODO : Naming
        var reqData = new
        {
            RtpObserverId = Guid.NewGuid().ToString(),
            maxEntries,
            threshold,
            interval
        };

        // Fire and forget
        await channel.Request("router.createAudioLevelObserver", @internal.RouterId, reqData);

        var audioLevelObserver = new AudioLevelObserver<TAudioLevelObserverAppData>(new()
        {
            Internal = new RtpObserverObserverInternal
            {
                RouterId      = @internal.RouterId,
                RtpObserverId = reqData.RtpObserverId
            },
            Channel         = channel,
            PayloadChannel  = payloadChannel,
            AppData         = appData,
            GetProducerById = producerId => producers.GetValueOrDefault(producerId)
        });
        rtpObservers[audioLevelObserver.Id] = audioLevelObserver;
        audioLevelObserver.On("@close", async _ =>
        {
            rtpObservers.Remove(audioLevelObserver.Id);
        });

        // Emit observer event.
        await Observer.SafeEmit("newrtpobserver", audioLevelObserver);

        return audioLevelObserver;
    }

    /// <summary>
    /// Check whether the given RTP capabilities can consume the given Producer.
    /// </summary>
    /// <returns></returns>
    public bool CanConsume(string producerId, RtpCapabilities rtpCapabilities)
    {
        var producer = producers.GetValueOrDefault(producerId);

        if (producer != null)
        {
            logger?.LogError(
                "canConsume() | Producer with id {ID} not found", producerId);

            return false;
        }

        try
        {
            return Ortc.CanConsume(producer!.ConsumableRtpParameters, rtpCapabilities);
        }
        catch (Exception e)
        {
            logger?.LogError("canConsume() | unexpected error: {E}", e);

            return false;
        }
    }
}