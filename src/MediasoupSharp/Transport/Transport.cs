using System.Runtime.Serialization;
using MediasoupSharp.Channel;
using MediasoupSharp.Consumer;
using MediasoupSharp.DataConsumer;
using MediasoupSharp.DataProducer;
using MediasoupSharp.Exceptions;
using MediasoupSharp.Producer;
using MediasoupSharp.RtpParameters;
using MediasoupSharp.SctpParameters;
using Microsoft.Extensions.Logging;

namespace MediasoupSharp.Transport;

internal abstract class Transport<TTransportAppData, TEvents, TObserverEvents>
    : EnhancedEventEmitter<TEvents>
    where TEvents : TransportEvents
    where TObserverEvents : TransportObserverEvents
{

    /// <summary>
    /// Internal data.
    /// </summary>
    protected readonly TransportInternal Internal;

    /// <summary>
    /// Transport data.
    /// </summary>
    private readonly TransportData data;

    /// <summary>
    /// Channel instance.
    /// </summary>
    protected readonly Channel.Channel Channel;

    /// <summary>
    /// PayloadChannel instance.
    /// </summary>
    protected readonly PayloadChannel.PayloadChannel PayloadChannel;

    /// <summary>
    /// Whether the Transport is closed.
    /// </summary>
    public bool Closed { get; private set; }

    /// <summary>
    /// App custom data.
    /// </summary>
    public TTransportAppData AppData { get; set; }

    /// <summary>
    /// Method to retrieve Router RTP capabilities.
    /// </summary>
    private readonly Func<RtpCapabilities> getRouterRtpCapabilities;

    /// <summary>
    /// Method to retrieve a Producer.
    /// </summary>
    protected readonly Func<string, Producer.Producer?> GetProducerById;

    /// <summary>
    /// Method to retrieve a DataProducer.
    /// </summary>
    protected readonly Func<string, DataProducer.DataProducer?> GetDataProducerById;

    /// <summary>
    /// Producers map.
    /// </summary>
    private readonly Dictionary<string, Producer.Producer> producers = new();

    /// <summary>
    /// Consumers map.
    /// </summary>
    protected readonly Dictionary<string, Consumer.Consumer> Consumers = new();

    /// <summary>
    /// DataProducers map.
    /// </summary>
    protected readonly Dictionary<string, DataProducer.DataProducer> DataProducers = new();

    /// <summary>
    /// DataConsumers map.
    /// </summary>
    protected readonly Dictionary<string, DataConsumer.DataConsumer> DataConsumers = new();

    /// <summary>
    /// RTCP CNAME for Producers.
    /// </summary>
    private string? cnameForProducers;

    /// <summary>
    /// Next MID for Consumers. It"s converted into string when used.
    /// </summary>
    private int nextMidForConsumers = 0;

    /// <summary>
    /// Buffer with available SCTP stream ids.
    /// </summary>
    private byte[]? sctpStreamIds;

    /// <summary>m
    /// Next SCTP stream id.
    /// </summary>
    private int nextSctpStreamId = 0;

    /// <summary>
    /// Observer instance.
    /// </summary>
    public EnhancedEventEmitter<TObserverEvents> Observer => observer ??= new();
    
    #region Extra

    private EnhancedEventEmitter<TObserverEvents>? observer;
    public override ILoggerFactory LoggerFactory
    {
        init
        {
            observer = new EnhancedEventEmitter<TObserverEvents>
            {
                LoggerFactory = value
            };
            base.LoggerFactory = value;
        }
    }

    #endregion
    protected Transport(
        TransportConstructorOptions<TTransportAppData> arg
    ) 
    {
        Internal = arg.Internal;
        data = arg.Data;
        Channel = arg.Channel;
        PayloadChannel = arg.PayloadChannel;
        AppData = arg.AppData ?? (TTransportAppData)FormatterServices.GetUninitializedObject(typeof(TTransportAppData));
        getRouterRtpCapabilities = arg.GetRouterRtpCapabilities;
        GetProducerById = arg.GetProducerById;
        GetDataProducerById = arg.GetDataProducerById;
    }

    public string Id => Internal.TransportId;

    internal Channel.Channel ChannelForTesting => Channel;

    /// <summary>
    /// Close the Transport.
    /// </summary>
    public void Close()
    {
        if (Closed)
        {
            return;
        }

        Logger?.LogDebug("CloseAsync() | Transport:{Id}", Id);

        Closed = true;

        // Remove notification subscriptions.
        Channel.RemoveAllListeners(Internal.TransportId);
        PayloadChannel.RemoveAllListeners(Internal.TransportId);

        // TODO : Naming
        var reqData = new { transportId = Internal.TransportId };

        // Fire and forget
        Channel.Request("router.closeTransport", Internal.RouterId, reqData)
            .ContinueWith(_ => { }, TaskContinuationOptions.OnlyOnFaulted);

        // Close every Producer.
        foreach (var producer in producers.Values)
        {
            producer.TransportClosed();

            // Must tell the Router.
            _ = Emit("@producerclose", producer);
        }

        producers.Clear();

        // Close every Consumer.
        foreach (var consumer in Consumers.Values)
        {
            consumer.TransportClosed();
        }

        Consumers.Clear();

        // Close every DataProducer.
        foreach (var dataProducer in DataProducers.Values)
        {
            dataProducer.TransportClosed();

            // Must tell the Router.
            _ = Emit("@dataproducerclose", dataProducer);
        }

        DataProducers.Clear();

        // Close every DataConsumer.
        foreach (var dataConsumer in DataConsumers.Values)
        {
            dataConsumer.TransportClosed();
        }

        DataConsumers.Clear();


        _ = Emit("@close");

        // Emit observer event.
        _ = Observer.SafeEmit("close");
    }

    /// <summary>
    /// Router was closed.
    /// </summary>
    public virtual void RouterClosed()
    {
        if (Closed)
        {
            return;
        }

        Logger?.LogDebug("RouterClosed() | Transport:{Id}", Id);

        Closed = true;

        // Remove notification subscriptions.
        Channel.RemoveAllListeners(Internal.TransportId);
        PayloadChannel.RemoveAllListeners(Internal.TransportId);

        // Close every Producer.
        foreach (var producer in producers.Values)
        {
            producer.TransportClosed();

            // NOTE: No need to tell the Router since it already knows (it has
            // been closed in fact).
        }

        producers.Clear();

        // Close every Consumer.
        foreach (var consumer in Consumers.Values)
        {
            consumer.TransportClosed();
        }

        Consumers.Clear();

        // Close every DataProducer.
        foreach (var dataProducer in DataProducers.Values)
        {
            dataProducer.TransportClosed();

            // NOTE: No need to tell the Router since it already knows (it has
            // been closed in fact).
        }

        DataProducers.Clear();

        // Close every DataConsumer.
        foreach (var dataConsumer in DataConsumers.Values)
        {
            dataConsumer.TransportClosed();
        }

        DataConsumers.Clear();


        _ = SafeEmit("routerclose");

        // Emit observer event.
        _ = Observer.SafeEmit("close");
    }

    /// <summary>
    /// Listen server was closed (this just happens in WebRtcTransports when their
    /// associated WebRtcServer is closed).
    /// @private
    /// </summary>
    private void ListenServerClosed()
    {
        if (Closed)
        {
            return;
        }

        Logger?.LogDebug("listenServerClosed()");

        Closed = true;

        // Remove notification subscriptions.
        Channel.RemoveAllListeners(Internal.TransportId);
        PayloadChannel.RemoveAllListeners(Internal.TransportId);

        // Close every Producer.
        foreach (var producer in producers.Values)
        {
            producer.TransportClosed();

            // NOTE: No need to tell the Router since it already knows (it has
            // been closed in fact).
        }

        producers.Clear();

        // Close every Consumer.
        foreach (var consumer in Consumers.Values)
        {
            consumer.TransportClosed();
        }

        Consumers.Clear();

        // Close every DataProducer.
        foreach (var dataProducer in DataProducers.Values)
        {
            dataProducer.TransportClosed();

            // NOTE: No need to tell the Router since it already knows (it has
            // been closed in fact).
        }

        DataProducers.Clear();

        // Close every DataConsumer.
        foreach (var dataConsumer in DataConsumers.Values)
        {
            dataConsumer.TransportClosed();
        }

        DataConsumers.Clear();

        // Need to emit this event to let the parent Router know since
        // transport.listenServerClosed() is called by the listen server.
        // NOTE: Currently there is just WebRtcServer for WebRtcTransports.
        _ = Emit("@listenserverclose");

        _ = SafeEmit("listenserverclose");

        // Emit observer event.
        _ = Observer.SafeEmit("close");
    }

    /// <summary>
    /// Dump Transport.
    /// </summary>
    public async Task<object> DumpAsync()
    {
        Logger?.LogDebug("DumpAsync() | Transport:{Id}", Id);

        return (await Channel.Request("transport.dump", Internal.TransportId))!;
    }

    /// <summary>
    /// Get Transport stats.
    /// </summary>
    public abstract Task<List<object>> GetStatsAsync();

    /// <summary>
    /// Provide the Transport remote parameters.
    /// </summary>
    /// <param name="parameters"></param>
    /// <returns></returns>
    public abstract Task ConnectAsync(object parameters);

    /// <summary>
    /// Set maximum incoming bitrate for receiving media.
    /// </summary>
    /// <param name="bitrate"></param>
    /// <returns></returns>
    public async Task SetMaxIncomingBitrateAsync(int bitrate)
    {
        Logger?.LogDebug("SetMaxIncomingBitrateAsync() | Transport:{Id} Bitrate:{Bitrate}", Id, bitrate);

        // TODO : Naming
        var reqData = new { bitrate };

        await Channel.Request("transport.setMaxIncomingBitrate", Internal.TransportId, reqData);
    }

    /// <summary>
    /// Set maximum outgoing bitrate for sending media.
    /// </summary>
    /// <param name="bitrate"></param>
    /// <returns></returns>
    public async Task SetMaxOutgoingBitrateAsync(int bitrate)
    {
        Logger?.LogDebug("setMaxOutgoingBitrate() | Transport:{Id} Bitrate:{Bitrate}", Id, bitrate);

        // TODO : Naming
        var reqData = new { bitrate };

        await Channel.Request("transport.setMaxOutgoingBitrate", Internal.TransportId, reqData);
    }

    /// <summary>
    /// Set minimum outgoing bitrate for sending media.
    /// </summary>
    /// <param name="bitrate"></param>
    public async Task SetMinOutgoingBitrate(int bitrate)
    {
        Logger?.LogDebug("setMinOutgoingBitrate() {Bitrate}", bitrate);

        var reqData = new { bitrate };

        await Channel.Request(
            "transport.setMinOutgoingBitrate", Internal.TransportId, reqData);
    }


    /// <summary>
    /// Create a Producer.
    /// </summary>
    public virtual async Task<Producer.Producer<TProducerAppData>> ProduceAsync<TProducerAppData>(
        ProducerOptions<TProducerAppData> producerOptions)
    {
        var id = producerOptions.Id;
        var kind = producerOptions.Kind;
        var rtpParameters = producerOptions.RtpParameters;
        var paused = producerOptions.Paused ?? false;
        var keyFrameRequestDelay = producerOptions.KeyFrameRequestDelay;
        var appData = producerOptions.AppData;

        Logger?.LogDebug("ProduceAsync() | Transport:{Id}", Id);

        if (!id.IsNullOrEmpty() && producers.ContainsKey(producerOptions.Id!))
        {
            throw new Exception($"a Producer with same id \"{producerOptions.Id}\" already exists");
        }
        else if (kind is not (MediaKind.audio or MediaKind.video))
        {
            throw new TypeError($"invalid kind {kind}");
        }

        // This may throw.
        ORTC.Ortc.ValidateRtpParameters(rtpParameters);

        // If missing or empty encodings, add one.
        if (!rtpParameters.Encodings.IsNullOrEmpty())
        {
            producerOptions.RtpParameters.Encodings = new List<RtpEncodingParameters>
            {
                new()
            };
        }

        // Don"t do this in PipeTransports since there we must keep CNAME value in
        // each Producer.
        // TODO: (alby) 反模式
        if (this is PipeTransport.PipeTransport)
        {
            // If CNAME is given and we don"t have yet a CNAME for Producers in this
            // Transport, take it.
            if (cnameForProducers.IsNullOrWhiteSpace()
                && producerOptions.RtpParameters.Rtcp != null
                && !producerOptions.RtpParameters.Rtcp.Cname.IsNullOrWhiteSpace())
            {
                cnameForProducers = producerOptions.RtpParameters.Rtcp.Cname;
            }
            // Otherwise if we don"t have yet a CNAME for Producers and the RTP parameters
            // do not include CNAME, create a random one.
            else if (cnameForProducers.IsNullOrWhiteSpace())
            {
                cnameForProducers = Guid.NewGuid().ToString()[..8];
            }

            // Override Producer"s CNAME.
            // 对 RtcpParameters 序列化时，CNAME 和 ReducedSize 为 null 会忽略，因为客户端库对其有校验。
            producerOptions.RtpParameters.Rtcp ??= new RtcpParameters();
            producerOptions.RtpParameters.Rtcp.Cname = cnameForProducers;
        }

        var routerRtpCapabilities = getRouterRtpCapabilities();

        // This may throw.
        var rtpMapping =
            ORTC.Ortc.GetProducerRtpParametersMapping(rtpParameters, routerRtpCapabilities);

        // This may throw.
        var consumableRtpParameters = ORTC.Ortc.GetConsumableRtpParameters(
            kind, rtpParameters, routerRtpCapabilities, rtpMapping);

        var reqData = new
        {
            ProducerId = id.IsNullOrWhiteSpace() ? Guid.NewGuid().ToString() : id!,
            kind,
            rtpParameters,
            rtpMapping,
            keyFrameRequestDelay,
            paused,
        };

        var status = await Channel.Request("transport.produce", Internal.TransportId, reqData) as dynamic;
        var data = new ProducerData
        {
            Kind = kind,
            RtpParameters = rtpParameters,
            Type = status!.Type,
            ConsumableRtpParameters = consumableRtpParameters
        };

        var producer = new Producer<TProducerAppData>(
            new ProducerInternal
            {
                RouterId = Internal.RouterId,
                TransportId = Internal.TransportId,
                ProducerId = reqData.ProducerId
            },
            data,
            Channel,
            PayloadChannel,
            appData,
            paused);

        producers[producer.Id] = producer;
        producer.On("@close", async _ =>
        {
            producers.Remove(producer.Id);
            var __ = Emit("@producerclose", producer);
        });

        _ = Emit("@newproducer", producer);

        // Emit observer event.
        _ = Observer.SafeEmit("newproducer", producer);

        return producer;
    }

    /// <summary>
    /// Create a Consumer.
    /// </summary>
    /// <param name="consumerOptions"></param>
    /// <returns></returns>
    public virtual async Task<Consumer.Consumer<TConsumerAppData>> ConsumeAsync<TConsumerAppData>(
        ConsumerOptions<TConsumerAppData> consumerOptions)
    {
        var producerId = consumerOptions.ProducerId;
        var rtpCapabilities = consumerOptions.RtpCapabilities;
        var paused = consumerOptions.Paused ?? false;
        var mid = consumerOptions.Mid;
        var preferredLayers = consumerOptions.PreferredLayers;
        var ignoreDtx = consumerOptions.IgnoreDtx ?? false;
        var enableRtx = consumerOptions.EnableRtx;
        var pipe = consumerOptions.Pipe ?? false;
        var appData = consumerOptions.AppData;

        Logger?.LogDebug("ConsumeAsync() | Transport:{Id}", Id);

        if (producerId.IsNullOrWhiteSpace())
        {
            throw new TypeError("missing producerId");
        }

        if (mid?.Length == 0)
        {
            throw new TypeError("if given, mid must be non empty string");
        }

        // This may throw.
        ORTC.Ortc.ValidateRtpCapabilities(consumerOptions.RtpCapabilities);

        var producer = GetProducerById(consumerOptions.ProducerId);

        if (producer == null)
        {
            throw new NullReferenceException($"Producer with id {consumerOptions.ProducerId} not found");
        }

        // If enableRtx is not given, set it to true if video and false if audio.
        enableRtx ??= producer.Kind == MediaKind.video;

        // TODO : Some changes here
        // This may throw.
        var rtpParameters = ORTC.Ortc.GetConsumerRtpParameters(
            producer.ConsumableRtpParameters,
            consumerOptions.RtpCapabilities,
            pipe);

        if (!pipe)
        {
            if (mid != null)
            {
                rtpParameters.Mid = mid;
            }
            else
            {
                // Set MID.
                rtpParameters.Mid = $"{nextMidForConsumers++}";

                // We use up to 8 bytes for MID (string).
                if (nextMidForConsumers == 100000000)
                {
                    Logger?.LogError("ConsumeAsync() | Reaching max MID value {NextMidForConsumers}",
                        nextMidForConsumers);

                    nextMidForConsumers = 0;
                }
            }
        }

        var reqData = new
        {
            ConsumerId = Guid.NewGuid().ToString(),
            ProducerId = producerId,
            producer.Kind,
            RtpParameters = rtpParameters,
            Type = (pipe ? ConsumerType.pipe : (ConsumerType)producer.Type).ToString(),
            ConsumableRtpEncodings = producer.ConsumableRtpParameters.Encodings,
            consumerOptions.Paused,
            consumerOptions.PreferredLayers,
            consumerOptions.IgnoreDtx,
        };

        var status = (await Channel.Request("transport.consume", Internal.TransportId, reqData) as dynamic)!;

        var data = new ConsumerData
        {
            ProducerId = consumerOptions.ProducerId,
            Kind = producer.Kind,
            RtpParameters = rtpParameters,
            Type = pipe
                ? ConsumerType.pipe
                : (ConsumerType)producer.Type // 注意：类型转换。ProducerType 的每一种值在 ConsumerType 都有对应且相同的值。
        };

        
        var consumer = new Consumer.Consumer<TConsumerAppData>(
            new ConsumerInternal
            {
                RouterId = Internal.RouterId,
                TransportId = Internal.TransportId,
                ConsumerId = reqData.ConsumerId
            },
            data,
            Channel,
            PayloadChannel,
            appData,
            // TODO : Naming
            status.Paused,
            status.ProducerPaused,
            status.Score,
            status.PreferredLayers);
        
        Consumers[consumer.Id] = consumer;
        
        consumer.On("@close", async _ =>
        {
            Consumers.Remove(consumer.Id);
        });
        consumer.On("@producerclose", async _ =>
        {
            Consumers.Remove(consumer.Id);
        });

        // Emit observer event.
        await Observer.SafeEmit("newconsumer", consumer);

        return consumer;
    }

    /// <summary>
    /// Create a DataProducer.
    /// </summary>
    /// <returns></returns>
    public async Task<DataProducer<TDataProducerAppData>> ProduceDataAsync<TDataProducerAppData>(
        DataProducerOptions<TDataProducerAppData> dataProducerOptions)
    {
        Logger?.LogDebug("ProduceDataAsync() | Transport:{Id}", Id);

        if (!dataProducerOptions.Id.IsNullOrWhiteSpace() && DataProducers.ContainsKey(dataProducerOptions.Id!))
        {
            throw new Exception($"A DataProducer with same id {dataProducerOptions.Id} already exists");
        }

        if (dataProducerOptions.Label.IsNullOrWhiteSpace())
        {
            dataProducerOptions.Label = string.Empty;
        }

        if (dataProducerOptions.Protocol.IsNullOrWhiteSpace())
        {
            dataProducerOptions.Protocol = string.Empty;
        }

        await using (await CloseLock.ReadLockAsync())
        {
            if (Closed)
            {
                throw new InvalidStateException("Transport closed");
            }

            DataProducerType type;
            // If this is not a DirectTransport, sctpStreamParameters are required.
            // TODO: (alby) 反模式
            if (GetType() != typeof(DirectTransport.DirectTransport))
            {
                type = DataProducerType.Sctp;

                // This may throw.
                ORTC.Ortc.ValidateSctpStreamParameters(dataProducerOptions.SctpStreamParameters!);
            }
            // If this is a DirectTransport, sctpStreamParameters must not be given.
            else
            {
                type = DataProducerType.Direct;

                if (dataProducerOptions.SctpStreamParameters != null)
                {
                    Logger?.LogWarning(
                        $"ProduceDataAsync() | Transport:{TransportId} sctpStreamParameters are ignored when producing data on a DirectTransport");
                }
            }

            var reqData = new
            {
                DataProducerId = dataProducerOptions.Id.IsNullOrWhiteSpace()
                    ? Guid.NewGuid().ToString()
                    : dataProducerOptions.Id,
                Type = type.GetDescription<EnumMemberAttribute>(x => x.Value!),
                dataProducerOptions.SctpStreamParameters,
                Label = dataProducerOptions.Label!,
                Protocol = dataProducerOptions.Protocol!
            };

            var resData =
                await Channel.RequestAsync(MethodId.TRANSPORT_PRODUCE_DATA, Internal.TransportId, reqData);
            var responseData = resData!.Deserialize<TransportDataProduceResponseData>()!;
            var data = new DataProducerData
            {
                SctpStreamParameters = responseData.SctpStreamParameters,
                Label = responseData.Label!,
                Protocol = responseData.Protocol!,
            };
            var dataProducer = new DataProducer.DataProducer(Logger?Factory,
                new DataProducerInternal(Internal.RouterId, Internal.TransportId, reqData.DataProducerId),
                data,
                Channel,
                PayloadChannel,
                AppData);

            dataProducer.On("@close", async (_, _) =>
            {
                await DataProducersLock.WaitAsync();
                try
                {
                    DataProducers.Remove(dataProducer.DataProducerId);
                }
                catch (Exception ex)
                {
                    Logger?.LogError(ex, "@close");
                }
                finally
                {
                    DataProducersLock.Set();
                }

                Emit("@dataproducerclose", dataProducer);
            });

            await DataProducersLock.WaitAsync();
            try
            {
                DataProducers[dataProducer.DataProducerId] = dataProducer;
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "ProduceDataAsync()");
            }
            finally
            {
                DataProducersLock.Set();
            }

            Emit("@newdataproducer", dataProducer);

            // Emit observer event.
            Observer.Emit("newdataproducer", dataProducer);

            return dataProducer;
        }
    }

    /// <summary>
    /// Create a DataConsumer.
    /// </summary>
    /// <param name="dataConsumerOptions"></param>
    /// <returns></returns>
    public async Task<DataConsumer.DataConsumer> ConsumeDataAsync(DataConsumerOptions dataConsumerOptions)
    {
        Logger?.LogDebug($"ConsumeDataAsync() | Transport:{TransportId}");

        if (dataConsumerOptions.DataProducerId.IsNullOrWhiteSpace())
        {
            throw new Exception("Missing dataProducerId");
        }

        var dataProducer = await GetDataProducerById(dataConsumerOptions.DataProducerId);
        if (dataProducer == null)
        {
            throw new Exception($"DataProducer with id {dataConsumerOptions.DataProducerId} not found");
        }

        await using (await CloseLock.ReadLockAsync())
        {
            if (Closed)
            {
                throw new InvalidStateException("Transport closed");
            }

            DataProducerType type;
            SctpStreamParameters? sctpStreamParameters = null;
            int sctpStreamId = -1;

            // If this is not a DirectTransport, use sctpStreamParameters from the
            // DataProducer (if type "sctp") unless they are given in method parameters.
            // TODO: (alby) 反模式
            if (GetType() != typeof(DirectTransport.DirectTransport))
            {
                type = DataProducerType.Sctp;

                sctpStreamParameters = dataProducer.data.SctpStreamParameters!.DeepClone();
                // This may throw.
                lock (sctpStreamIdsLock)
                {
                    sctpStreamId = GetNextSctpStreamId();

                    if (sctpStreamIds == null || sctpStreamId > sctpStreamIds.Length - 1)
                    {
                        throw new IndexOutOfRangeException(nameof(sctpStreamIds));
                    }

                    sctpStreamIds[sctpStreamId] = 1;
                    sctpStreamParameters.StreamId = sctpStreamId;
                }
            }
            // If this is a DirectTransport, sctpStreamParameters must not be used.
            else
            {
                type = DataProducerType.Direct;

                if (dataConsumerOptions.Ordered.HasValue ||
                    dataConsumerOptions.MaxPacketLifeTime.HasValue ||
                    dataConsumerOptions.MaxRetransmits.HasValue
                   )
                {
                    Logger?.LogWarning(
                        "ConsumeDataAsync() | Ordered, maxPacketLifeTime and maxRetransmits are ignored when consuming data on a DirectTransport");
                }
            }

            var reqData = new
            {
                DataConsumerId = Guid.NewGuid().ToString(),
                DataProducerId = dataConsumerOptions.DataProducerId,
                Type = type.GetDescription<EnumMemberAttribute>(x => x.Value!),
                SctpStreamParameters = sctpStreamParameters,
                Label = dataProducer.data.Label,
                Protocol = dataProducer.data.Protocol,
            };

            var resData =
                await Channel.RequestAsync(MethodId.TRANSPORT_CONSUME_DATA, Internal.TransportId, reqData);
            var responseData = resData!.Deserialize<TransportDataConsumeResponseData>()!;

            var dataConsumer = new DataConsumer.DataConsumer(Logger?Factory,
                new DataConsumerInternal(Internal.RouterId, Internal.TransportId, reqData.DataConsumerId),
                responseData, // 直接使用返回值
                Channel,
                PayloadChannel,
                AppData);

            dataConsumer.On("@close", async (_, _) =>
            {
                await DataConsumersLock.WaitAsync();
                try
                {
                    DataConsumers.Remove(dataConsumer.DataConsumerId);
                    lock (sctpStreamIdsLock)
                    {
                        if (sctpStreamIds != null && sctpStreamId >= 0)
                        {
                            sctpStreamIds[sctpStreamId] = 0;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger?.LogError(ex, "@close");
                }
                finally
                {
                    DataConsumersLock.Set();
                }
            });

            dataConsumer.On("@dataproducerclose", async (_, _) =>
            {
                await DataConsumersLock.WaitAsync();
                try
                {
                    DataConsumers.Remove(dataConsumer.DataConsumerId);
                    lock (sctpStreamIdsLock)
                    {
                        if (sctpStreamIds != null && sctpStreamId >= 0)
                        {
                            sctpStreamIds[sctpStreamId] = 0;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger?.LogError(ex, "@dataproducerclose");
                }
                finally
                {
                    DataConsumersLock.Set();
                }
            });

            await DataConsumersLock.WaitAsync();
            try
            {
                DataConsumers[dataConsumer.DataConsumerId] = dataConsumer;
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "ConsumeDataAsync()");
            }
            finally
            {
                DataConsumersLock.Set();
            }

            // Emit observer event.
            Observer.Emit("newdataconsumer", dataConsumer);

            return dataConsumer;
        }
    }

    /// <summary>
    /// Enable "trace" event.
    /// </summary>
    /// <param name="types"></param>
    /// <returns></returns>
    public async Task EnableTraceEventAsync(TransportTraceEventType[] types)
    {
        Logger?.LogDebug($"EnableTraceEventAsync() | Transport:{TransportId}");

        using (await CloseLock.ReadLockAsync())
        {
            if (Closed)
            {
                throw new InvalidStateException("Transport closed");
            }

            var reqData = new { Types = types };
            // Fire and forget
            Channel.RequestAsync(MethodId.TRANSPORT_ENABLE_TRACE_EVENT, Internal.TransportId, reqData)
                .ContinueWithOnFaultedHandleLog(Logger?);
        }
    }

    #region Private Methods

    private int GetNextSctpStreamId()
    {
        if (BaseData.SctpParameters == null)
        {
            throw new Exception("Missing data.sctpParameters.MIS");
        }

        if (sctpStreamIds == null)
        {
            throw new Exception(nameof(sctpStreamIds));
        }

        var numStreams = BaseData.SctpParameters.MIS;

        if (sctpStreamIds.IsNullOrEmpty())
        {
            sctpStreamIds = new int[numStreams];
        }

        int sctpStreamId;

        for (var idx = 0; idx < sctpStreamIds.Length; ++idx)
        {
            sctpStreamId = (nextSctpStreamId + idx) % sctpStreamIds.Length;

            if (sctpStreamIds[sctpStreamId] == 0)
            {
                nextSctpStreamId = sctpStreamId + 1;
                return sctpStreamId;
            }
        }

        throw new Exception("No sctpStreamId available");
    }

    #endregion Private Methods
}