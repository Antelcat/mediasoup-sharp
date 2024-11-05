using Antelcat.AutoGen.ComponentModel.Diagnostic;
using Antelcat.MediasoupSharp.Internals.Extensions;
using FBS.Request;
using FBS.Router;
using FBS.SctpParameters;
using FBS.Transport;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Threading;

namespace Antelcat.MediasoupSharp;

using RouterObserver = EnhancedEventEmitter<RouterObserverEvents>;

public class RouterOptions<TRouterAppData>
{
    /// <summary>
    /// Router media codecs.
    /// </summary>
    public RtpCodecCapability[]? MediaCodecs { get; set; }

    /// <summary>
    /// Custom application data.
    /// </summary>
    public TRouterAppData? AppData { get; set; }
}

public class PipeToRouterOptions
{
    /// <summary>
    /// The id of the Producer to consume.
    /// </summary>
    public string? ProducerId { get; set; }

    /// <summary>
    /// The id of the DataProducer to consume.
    /// </summary>
    public string? DataProducerId { get; set; }

    /// <summary>
    /// Target Router instance.
    /// </summary>
    public IRouter Router { get; set; }

    /// <summary>
    /// Listenning Infomation.
    /// </summary>
    public ListenInfoT ListenInfo { get; set; }

    /// <summary>
    /// Create a SCTP association. Default true.
    /// </summary>
    public bool EnableSctp { get; set; } = true;

    /// <summary>
    /// SCTP streams number.
    /// </summary>
    public NumSctpStreamsT NumSctpStreams { get; set; } = new() { Os = 1024, Mis = 1024 };

    /// <summary>
    /// Enable RTX and NACK for RTP retransmission.
    /// </summary>
    public bool EnableRtx { get; set; }

    /// <summary>
    /// Enable SRTP.
    /// </summary>
    public bool EnableSrtp { get; set; }
}

public class PipeToRouterResult
{
    /// <summary>
    /// The Consumer created in the current Router.
    /// </summary>
    public IConsumer? PipeConsumer { get; set; }

    /// <summary>
    /// The Producer created in the target Router.
    /// </summary>
    public IProducer? PipeProducer { get; set; }

    /// <summary>
    /// The DataConsumer created in the current Router.
    /// </summary>
    public IDataConsumer? PipeDataConsumer { get; set; }

    /// <summary>
    /// The DataProducer created in the target Router.
    /// </summary>
    public IDataProducer? PipeDataProducer { get; set; }
}

public abstract class RouterEvents
{
    public object? workerclose;

    public (string, Exception)? listenererror;

    // Private events.
    internal object? _close;
}

public abstract class RouterObserverEvents
{
    public object?      close;
    public ITransport   newtransport;
    public IRtpObserver newrtpobserver;
}

public class RouterInternal
{
    public required string RouterId { get; set; }
}

public class RouterData
{
    public RtpCapabilities RtpCapabilities { get; set; }
}

[AutoExtractInterface]
public sealed class Router<TRouterAppData> : EnhancedEventEmitter<RouterEvents>, IEquatable<IRouter>, IRouter
    where TRouterAppData : new()
{
    /// <summary>
    /// Logger.
    /// </summary>
    private readonly ILogger logger = new Logger<Router<TRouterAppData>>();

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
    private readonly Dictionary<string, ITransport> transports = [];

    private readonly AsyncReaderWriterLock transportsLock = new();

    /// <summary>
    /// Producers map.
    /// </summary>
    private readonly Dictionary<string, IProducer> producers = [];

    private readonly AsyncReaderWriterLock producersLock = new();

    /// <summary>
    /// RtpObservers map.
    /// </summary>
    private readonly Dictionary<string, IRtpObserver> rtpObservers = [];

    private readonly AsyncReaderWriterLock rtpObserversLock = new();

    /// <summary>
    /// DataProducers map.
    /// </summary>
    private readonly Dictionary<string, IDataProducer> dataProducers = [];

    private readonly AsyncReaderWriterLock dataProducersLock = new();

    /// <summary>
    /// Router to PipeTransport map.
    /// </summary>
    private readonly Dictionary<IRouter, IPipeTransport[]> mapRouterPipeTransports = [];

    private readonly AsyncReaderWriterLock mapRouterPipeTransportsLock = new();

    /// <summary>
    /// App custom data.
    /// </summary>
    public TRouterAppData AppData { get; }

    /// <summary>
    /// Observer instance.
    /// </summary>
    public RouterObserver Observer { get; } = new();

    /// <summary>
    /// <para>Events:</para>
    /// <para>@emits <see cref="RouterEvents.workerclose"/></para>
    /// <para>@emits <see cref="RouterEvents._close"/></para>
    /// <para>Observer events:</para>
    /// <para>@emits <see cref="RouterObserverEvents.close"/></para>
    /// <para>@emits <see cref="RouterObserverEvents.newtransport"/> - (transport: Transport)</para>
    /// <para>@emits <see cref="RouterObserverEvents.newrtpobserver"/> - (rtpObserver: RtpObserver)</para>
    /// </summary>
    public Router(
        RouterInternal @internal,
        RouterData data,
        IChannel channel,
        TRouterAppData? appData
    )
    {
        this.@internal = @internal;
        this.channel   = channel;
        Data           = data;
        AppData        = appData ?? new();
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

            this.Emit(static x => x._close);

            // Emit observer event.
            Observer.Emit(static x => x.close);
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

            this.Emit(static x => x.workerclose);

            // Emit observer event.
            Observer.Emit(static x => x.close);
        }
    }

    /// <summary>
    /// Dump Router.
    /// </summary>
    public async Task<FBS.Router.DumpResponseT> DumpAsync()
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
    public async Task<WebRtcTransport<TWebRtcTransportAppData>> CreateWebRtcTransportAsync<TWebRtcTransportAppData>(
        WebRtcTransportOptions<TWebRtcTransportAppData> options)
        where TWebRtcTransportAppData : new()
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
                    .Select(static m => new ListenInfoT
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

            var baseTransportOptions = new OptionsT
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

            var transport = new WebRtcTransport<TWebRtcTransportAppData>(
                new(data)
                {
                    Internal = new()
                    {
                        RouterId    = @internal.RouterId,
                        TransportId = transportId
                    },
                    Channel                  = channel,
                    AppData                  = appData,
                    GetRouterRtpCapabilities = () => Data.RtpCapabilities,
                    GetProducerById = async m =>
                    {
                        await using (await producersLock.ReadLockAsync())
                        {
                            return producers.GetValueOrDefault(m);
                        }
                    },
                    GetDataProducerById = async m =>
                    {
                        await using (await dataProducersLock.ReadLockAsync())
                        {
                            return dataProducers.GetValueOrDefault(m);
                        }
                    }
                });

            await ConfigureTransportAsync(transport, webRtcServer);

            return transport;
        }
    }

    /// <summary>
    /// Create a PlainTransport.
    /// </summary>
    public async Task<PlainTransport<TPlainTransportAppData>> CreatePlainTransportAsync<TPlainTransportAppData>(
        PlainTransportOptions<TPlainTransportAppData> options)
        where TPlainTransportAppData : new()
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
                EnableSctp                      = options.EnableSctp ?? false,
                NumSctpStreams                  = options.NumSctpStreams,
                MaxSctpMessageSize              = options.MaxSctpMessageSize ?? 262144,
                SctpSendBufferSize              = options.SctpSendBufferSize ?? 262144,
                IsDataChannel                   = false
            };

            var plainTransportOptions = new FBS.PlainTransport.PlainTransportOptionsT
            {
                Base            = baseTransportOptions,
                ListenInfo      = options.ListenInfo,
                RtcpListenInfo  = options.RtcpListenInfo,
                RtcpMux         = options.RtcpMux    ?? true,
                Comedia         = options.Comedia    ?? false,
                EnableSrtp      = options.EnableSrtp ?? false,
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

            var transport = new PlainTransport<TPlainTransportAppData>(
                new(data)
                {
                    Internal = new()
                    {
                        RouterId    = @internal.RouterId,
                        TransportId = transportId
                    },
                    Channel                  = channel,
                    GetRouterRtpCapabilities = () => Data.RtpCapabilities,
                    GetProducerById = async m =>
                    {
                        await using (await producersLock.ReadLockAsync())
                        {
                            return producers.GetValueOrDefault(m);
                        }
                    },
                    GetDataProducerById = async m =>
                    {
                        await using (await dataProducersLock.ReadLockAsync())
                        {
                            return dataProducers.GetValueOrDefault(m);
                        }
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
    public async Task<PipeTransport<TPipeTransportAppData>> CreatePipeTransportAsync<TPipeTransportAppData>(
        PipeTransportOptions<TPipeTransportAppData> options)
        where TPipeTransportAppData : new()
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

            var transport = new PipeTransport<TPipeTransportAppData>(
                new(data)
                {
                    Internal = new()
                    {
                        RouterId    = @internal.RouterId,
                        TransportId = transportId
                    },
                    Channel                  = channel,
                    AppData                  = options.AppData,
                    GetRouterRtpCapabilities = () => Data.RtpCapabilities,
                    GetProducerById = async m =>
                    {
                        await using (await producersLock.ReadLockAsync())
                        {
                            return producers.GetValueOrDefault(m);
                        }
                    },
                    GetDataProducerById = async m =>
                    {
                        await using (await dataProducersLock.ReadLockAsync())
                        {
                            return dataProducers.GetValueOrDefault(m);
                        }
                    }
                });

            await ConfigureTransportAsync(transport);

            return transport;
        }
    }

    /// <summary>
    /// Create a DirectTransport.
    /// </summary>
    public async Task<DirectTransport<TDirectTransportAppData>> CreateDirectTransportAsync<TDirectTransportAppData>(
        DirectTransportOptions<TDirectTransportAppData> options)
        where TDirectTransportAppData : new()
    {
        logger.LogDebug("CreateDirectTransportAsync()");

        await using (await closeLock.ReadLockAsync())
        {
            if (closed)
            {
                throw new InvalidStateException("Router closed");
            }

            var transportId = Guid.NewGuid().ToString();

            var baseTransportOptions = new FBS.Transport.OptionsT
            {
                Direct         = true,
                MaxMessageSize = options.MaxMessageSize,
            };

            var directTransportOptions = new FBS.DirectTransport.DirectTransportOptionsT
            {
                Base = baseTransportOptions,
            };

            // Build Request
            var bufferBuilder = channel.BufferPool.Get();

            var createDirectTransportRequest = new CreateDirectTransportRequestT
            {
                TransportId = transportId,
                Options     = directTransportOptions
            };

            var createDirectTransportRequestOffset =
                CreateDirectTransportRequest.Pack(bufferBuilder, createDirectTransportRequest);

            var response = await channel.RequestAsync(bufferBuilder,
                Method.ROUTER_CREATE_DIRECTTRANSPORT,
                Body.Router_CreateDirectTransportRequest,
                createDirectTransportRequestOffset.Value,
                @internal.RouterId);

            /* Decode Response. */
            var data = response!.Value.BodyAsDirectTransport_DumpResponse().UnPack();

            var transport = new DirectTransport<TDirectTransportAppData>(
                new(data)
                {
                    Internal = new()
                    {
                        RouterId    = Id,
                        TransportId = transportId
                    },
                    Channel                  = channel,
                    AppData                  = options.AppData,
                    GetRouterRtpCapabilities = () => Data.RtpCapabilities,
                    GetProducerById = async m =>
                    {
                        await using (await producersLock.ReadLockAsync())
                        {
                            return producers.GetValueOrDefault(m);
                        }
                    },
                    GetDataProducerById = async m =>
                    {
                        await using (await dataProducersLock.ReadLockAsync())
                        {
                            return dataProducers.GetValueOrDefault(m);
                        }
                    }
                }
            );

            await ConfigureTransportAsync(transport);

            return transport;
        }
    }

    private async Task ConfigureTransportAsync(ITransport transport, IWebRtcServer? webRtcServer = null)
    {
        await using (await transportsLock.WriteLockAsync())
        {
            transports[transport.Id] = transport;
        }

        transport.On(static x => x._close, async _ =>
        {
            await using (await transportsLock.WriteLockAsync())
            {
                transports.Remove(transport.Id);
            }
        });
        transport.On(static x => x._listenserverclose, async _ =>
        {
            await using (await transportsLock.WriteLockAsync())
            {
                transports.Remove(transport.Id);
            }
        });
        transport.On(static x => x._newproducer, async producer =>
        {
            await using (await producersLock.WriteLockAsync())
            {
                producers[producer.Id] = producer;
            }
        });
        transport.On(static x => x._producerclose, async producer =>
        {
            await using (await producersLock.WriteLockAsync())
            {
                producers.Remove(producer.Id);
            }
        });
        transport.On(static x => x._newdataproducer, async dataProducer =>
        {
            await using (await dataProducersLock.WriteLockAsync())
            {
                dataProducers[dataProducer.Id] = dataProducer;
            }
        });
        transport.On(static x => x._dataproducerclose, async dataProducer =>
        {
            await using (await dataProducersLock.WriteLockAsync())
            {
                dataProducers.Remove(dataProducer.Id);
            }
        });

        // Emit observer event.
        Observer.Emit(static x => x.newtransport, transport);

        if (webRtcServer != null && transport is IWebRtcTransport webRtcTransport)
        {
            await webRtcServer.HandleWebRtcTransportAsync(webRtcTransport);
        }
    }

    /// <summary>
    /// Pipes the given Producer or DataProducer into another Router in same host.
    /// </summary>
    /// <param name="pipeToRouterOptions">ListenIp 传入 127.0.0.1, EnableSrtp 传入 true 。</param>
    ///
    // public async Task<PipeToRouterResult> PipeToRouteAsync(PipeToRouterOptions pipeToRouterOptions)
    // {
    //     await using (await closeLock.ReadLockAsync())
    //     {
    //         if (closed)
    //         {
    //             throw new InvalidStateException("Router closed");
    //         }
    //
    //         if (pipeToRouterOptions.ListenInfo == null)
    //         {
    //             throw new ArgumentNullException(nameof(pipeToRouterOptions), "Missing listenInfo");
    //         }
    //
    //         if (pipeToRouterOptions.ProducerId.IsNullOrWhiteSpace() &&
    //             pipeToRouterOptions.DataProducerId.IsNullOrWhiteSpace())
    //         {
    //             throw new ArgumentException("Missing producerId or dataProducerId");
    //         }
    //
    //         if (!pipeToRouterOptions.ProducerId.IsNullOrWhiteSpace() &&
    //             !pipeToRouterOptions.DataProducerId.IsNullOrWhiteSpace())
    //         {
    //             throw new ArgumentException("Just producerId or dataProducerId can be given");
    //         }
    //
    //         if (pipeToRouterOptions.Router == null)
    //         {
    //             throw new ArgumentNullException(nameof(pipeToRouterOptions), "Router not found");
    //         }
    //
    //         if (pipeToRouterOptions.Router == this)
    //         {
    //             throw new ArgumentException("Cannot use this Router as destination");
    //         }
    //
    //         IProducer?         producer     = null;
    //         IDataProducer? dataProducer = null;
    //
    //         if (!pipeToRouterOptions.ProducerId.IsNullOrWhiteSpace())
    //         {
    //             await using (await producersLock.ReadLockAsync())
    //             {
    //                 if (!producers.TryGetValue(pipeToRouterOptions.ProducerId!, out producer))
    //                 {
    //                     throw new Exception("Producer not found");
    //                 }
    //             }
    //         }
    //         else if (!pipeToRouterOptions.DataProducerId.IsNullOrWhiteSpace())
    //         {
    //             await using (await dataProducersLock.ReadLockAsync())
    //             {
    //                 if (!dataProducers.TryGetValue(pipeToRouterOptions.DataProducerId!, out dataProducer))
    //                 {
    //                     throw new Exception("DataProducer not found");
    //                 }
    //             }
    //         }
    //
    //         // Here we may have to create a new PipeTransport pair to connect source and
    //         // destination Routers. We just want to keep a PipeTransport pair for each
    //         // pair of Routers. Since this operation is async, it may happen that two
    //         // simultaneous calls to router1.pipeToRouter({ producerId: xxx, router: router2 })
    //         // would end up generating two pairs of PipeTranports. To prevent that, let's
    //         // use an async queue.
    //
    //         IPipeTransport? localPipeTransport  = null;
    //         IPipeTransport? remotePipeTransport = null;
    //
    //         // 因为有可能新增，所以用写锁。
    //         await using (await mapRouterPipeTransportsLock.WriteLockAsync())
    //         {
    //             if (mapRouterPipeTransports.TryGetValue(pipeToRouterOptions.Router, out var pipeTransportPair))
    //             {
    //                 localPipeTransport  = pipeTransportPair[0];
    //                 remotePipeTransport = pipeTransportPair[1];
    //             }
    //             else
    //             {
    //                 try
    //                 {
    //                     var pipeTransports = await Task.WhenAll(CreatePipeTransportAsync(new PipeTransportOptions
    //                         {
    //                             ListenInfo     = pipeToRouterOptions.ListenInfo,
    //                             EnableSctp     = pipeToRouterOptions.EnableSctp,
    //                             NumSctpStreams = pipeToRouterOptions.NumSctpStreams,
    //                             EnableRtx      = pipeToRouterOptions.EnableRtx,
    //                             EnableSrtp     = pipeToRouterOptions.EnableSrtp
    //                         }),
    //                         pipeToRouterOptions.Router.CreatePipeTransportAsync(new PipeTransportOptions
    //                         {
    //                             ListenInfo     = pipeToRouterOptions.ListenInfo,
    //                             EnableSctp     = pipeToRouterOptions.EnableSctp,
    //                             NumSctpStreams = pipeToRouterOptions.NumSctpStreams,
    //                             EnableRtx      = pipeToRouterOptions.EnableRtx,
    //                             EnableSrtp     = pipeToRouterOptions.EnableSrtp
    //                         })
    //                     );
    //
    //                     localPipeTransport  = pipeTransports[0];
    //                     remotePipeTransport = pipeTransports[1];
    //
    //                     await Task.WhenAll(localPipeTransport.ConnectAsync(new FBS.PipeTransport.ConnectRequestT
    //                         {
    //                             Ip             = remotePipeTransport.Data.Tuple.LocalAddress,
    //                             Port           = remotePipeTransport.Data.Tuple.LocalPort,
    //                             SrtpParameters = remotePipeTransport.Data.SrtpParameters,
    //                         }),
    //                         remotePipeTransport.ConnectAsync(new FBS.PipeTransport.ConnectRequestT
    //                         {
    //                             Ip             = localPipeTransport.Data.Tuple.LocalAddress,
    //                             Port           = localPipeTransport.Data.Tuple.LocalPort,
    //                             SrtpParameters = localPipeTransport.Data.SrtpParameters,
    //                         })
    //                     );
    //
    //                     localPipeTransport.Observer.On("close", async _ =>
    //                     {
    //                         await remotePipeTransport.CloseAsync();
    //                         await using (await mapRouterPipeTransportsLock.WriteLockAsync())
    //                         {
    //                             mapRouterPipeTransports.Remove(pipeToRouterOptions.Router);
    //                         }
    //                     });
    //
    //                     remotePipeTransport.Observer.On("close", async _ =>
    //                     {
    //                         await localPipeTransport.CloseAsync();
    //                         await using (await mapRouterPipeTransportsLock.WriteLockAsync())
    //                         {
    //                             mapRouterPipeTransports.Remove(pipeToRouterOptions.Router);
    //                         }
    //                     });
    //
    //                     mapRouterPipeTransports[pipeToRouterOptions.Router] =
    //                     [
    //                         localPipeTransport, remotePipeTransport
    //                     ];
    //                 }
    //                 catch (Exception ex)
    //                 {
    //                     logger.LogError(ex, "PipeToRouterAsync() | Create PipeTransport pair failed.");
    //
    //                     if (localPipeTransport != null)
    //                     {
    //                         await localPipeTransport.CloseAsync();
    //                     }
    //
    //                     if (remotePipeTransport != null)
    //                     {
    //                         await remotePipeTransport.CloseAsync();
    //                     }
    //
    //                     throw;
    //                 }
    //             }
    //         }
    //
    //         if (producer != null)
    //         {
    //             IConsumer? pipeConsumer = null;
    //             IProducer? pipeProducer = null;
    //
    //             try
    //             {
    //                 pipeConsumer = await localPipeTransport.ConsumeAsync(new ConsumerOptions
    //                 {
    //                     ProducerId = pipeToRouterOptions.ProducerId!
    //                 });
    //
    //                 pipeProducer = await remotePipeTransport.ProduceAsync(new ProducerOptions
    //                 {
    //                     Id            = producer.Id,
    //                     Kind          = pipeConsumer.Data.Kind,
    //                     RtpParameters = pipeConsumer.Data.RtpParameters,
    //                     Paused        = pipeConsumer.ProducerPaused,
    //                     AppData       = producer.AppData,
    //                 });
    //
    //                 // Ensure that the producer has not been closed in the meanwhile.
    //                 if (producer.Closed)
    //                     throw new InvalidStateException("original Producer closed");
    //
    //                 // Ensure that producer.paused has not changed in the meanwhile and, if
    //                 // so, sync the pipeProducer.
    //                 if (pipeProducer.Paused != producer.Paused)
    //                 {
    //                     if (producer.Paused)
    //                         await pipeProducer.PauseAsync();
    //                     else
    //                         await pipeProducer.ResumeAsync();
    //                 }
    //
    //                 // Pipe events from the pipe Consumer to the pipe Producer.
    //                 pipeConsumer.Observer.On("close", async _ => await pipeProducer.CloseAsync());
    //                 pipeConsumer.Observer.On("pause", async _ => await pipeProducer.PauseAsync());
    //                 pipeConsumer.Observer.On("resume", async _ => await pipeProducer.ResumeAsync());
    //
    //                 // Pipe events from the pipe Producer to the pipe Consumer.
    //                 pipeProducer.Observer.On("close", async _ => await pipeConsumer.CloseAsync());
    //
    //                 return new PipeToRouterResult { PipeConsumer = pipeConsumer, PipeProducer = pipeProducer };
    //             }
    //             catch (Exception ex)
    //             {
    //                 logger.LogError(ex, "PipeToRouterAsync() | Create pipe Consumer/Producer pair failed");
    //
    //                 if (pipeConsumer != null)
    //                 {
    //                     await pipeConsumer.CloseAsync();
    //                 }
    //
    //                 if (pipeProducer != null)
    //                 {
    //                     await pipeProducer.CloseAsync();
    //                 }
    //
    //                 throw;
    //             }
    //         }
    //
    //         if (dataProducer != null)
    //         {
    //             DataConsumer.DataConsumer? pipeDataConsumer = null;
    //             DataProducer.DataProducer? pipeDataProducer = null;
    //
    //             try
    //             {
    //                 pipeDataConsumer = await localPipeTransport.ConsumeDataAsync(new DataConsumerOptions
    //                 {
    //                     DataProducerId = pipeToRouterOptions.DataProducerId!
    //                 });
    //
    //                 pipeDataProducer = await remotePipeTransport.ProduceDataAsync(new DataProducerOptions
    //                 {
    //                     Id                   = dataProducer.Id,
    //                     SctpStreamParameters = pipeDataConsumer.Data.SctpStreamParameters,
    //                     Label                = pipeDataConsumer.Data.Label,
    //                     Protocol             = pipeDataConsumer.Data.Protocol,
    //                     AppData              = dataProducer.AppData,
    //                 });
    //
    //                 // Pipe events from the pipe DataConsumer to the pipe DataProducer.
    //                 pipeDataConsumer.Observer.On("close", async _ => await pipeDataProducer.CloseAsync());
    //
    //                 // Pipe events from the pipe DataProducer to the pipe DataConsumer.
    //                 pipeDataProducer.Observer.On("close", async _ => await pipeDataConsumer.CloseAsync());
    //
    //                 return new PipeToRouterResult
    //                     { PipeDataConsumer = pipeDataConsumer, PipeDataProducer = pipeDataProducer };
    //             }
    //             catch (Exception ex)
    //             {
    //                 logger.LogError(ex, "PipeToRouterAsync() | Create pipe DataConsumer/DataProducer pair failed.");
    //
    //                 if (pipeDataConsumer != null)
    //                 {
    //                     await pipeDataConsumer.CloseAsync();
    //                 }
    //
    //                 if (pipeDataProducer != null)
    //                 {
    //                     await pipeDataProducer.CloseAsync();
    //                 }
    //
    //                 throw;
    //             }
    //         }
    //
    //         throw new Exception("Internal error");
    //     }
    // }

    /// <summary>
    /// Create an ActiveSpeakerObserver
    /// </summary>
    public async Task<ActiveSpeakerObserver<TActiveSpeakerObserverAppData>> CreateActiveSpeakerObserverAsync<
        TActiveSpeakerObserverAppData>(
        ActiveSpeakerObserverOptions<TActiveSpeakerObserverAppData> activeSpeakerObserverOptions)
        where TActiveSpeakerObserverAppData : new()
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

            var activeSpeakerObserver = new ActiveSpeakerObserver<TActiveSpeakerObserverAppData>(
                new()
                {
                    Internal = new()
                    {
                        RouterId      = @internal.RouterId,
                        RtpObserverId = rtpObserverId
                    },
                    Channel = channel,
                    AppData = activeSpeakerObserverOptions.AppData,
                    GetProducerById = async m =>
                    {
                        await using (await producersLock.ReadLockAsync())
                        {
                            return producers.GetValueOrDefault(m);
                        }
                    }
                });

            await ConfigureRtpObserverAsync(activeSpeakerObserver);

            return activeSpeakerObserver;
        }
    }

    /// <summary>
    /// Create an AudioLevelObserver.
    /// </summary>
    public async Task<AudioLevelObserver<TAudioLevelObserverAppData>> CreateAudioLevelObserverAsync<
        TAudioLevelObserverAppData>(
        AudioLevelObserverOptions<TAudioLevelObserverAppData> audioLevelObserverOptions)
        where TAudioLevelObserverAppData : new()
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

            var audioLevelObserver = new AudioLevelObserver<TAudioLevelObserverAppData>(
                new()
                {
                    Internal = new()
                    {
                        RouterId      = @internal.RouterId,
                        RtpObserverId = rtpObserverId
                    },
                    Channel = channel,
                    AppData = audioLevelObserverOptions.AppData,
                    GetProducerById = async m =>
                    {
                        await using (await producersLock.ReadLockAsync())
                        {
                            return producers.GetValueOrDefault(m);
                        }
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
                logger.LogError("CanConsume() | Producer with id producerId:{ProducerId} not found", producerId);
                return false;
            }

            try
            {
                return Ortc.CanConsume(producer.Data.ConsumableRtpParameters, rtpCapabilities);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "CanConsume() | Unexpected error");
                return false;
            }
        }
    }

    #region IEquatable<T>

    public bool Equals(IRouter? other)
    {
        if (other is null)
        {
            return false;
        }

        return Id == other.Id;
    }

    public override bool Equals(object? other)
    {
        return Equals(other as IRouter);
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

    private Task ConfigureRtpObserverAsync(IRtpObserver rtpObserver)
    {
        rtpObservers[rtpObserver.Internal.RtpObserverId] = rtpObserver;
        rtpObserver.On(static x => x._close, async _ =>
        {
            await using (await rtpObserversLock.WriteLockAsync())
            {
                rtpObservers.Remove(rtpObserver.Internal.RtpObserverId);
            }
        });

        // Emit observer event.
        Observer.Emit(static x => x.newrtpobserver, rtpObserver);

        return Task.CompletedTask;
    }
}