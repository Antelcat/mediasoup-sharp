using System.Reflection;
using Antelcat.AutoGen.ComponentModel.Diagnostic;
using Antelcat.MediasoupSharp.Internals.Extensions;
using Antelcat.MediasoupSharp.FBS.DirectTransport;
using Antelcat.MediasoupSharp.FBS.Request;
using Antelcat.MediasoupSharp.FBS.SctpAssociation;
using Antelcat.MediasoupSharp.FBS.SctpParameters;
using Antelcat.MediasoupSharp.FBS.Transport;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Threading;

namespace Antelcat.MediasoupSharp;

public class TransportConstructorOptions<TTransportAppData>(TransportBaseData data)
{
    public required TransportInternal                  Internal                 { get; set; }
    public virtual  TransportBaseData                  Data                     { get; } = data;
    public required IChannel                           Channel                  { get; set; }
    public          TTransportAppData?                 AppData                  { get; set; }
    public required Func<RtpCapabilities>              GetRouterRtpCapabilities { get; set; }
    public required Func<string, Task<IProducer?>>     GetProducerById          { get; set; }
    public required Func<string, Task<IDataProducer?>> GetDataProducerById      { get; set; }
}

public class TransportInternal : RouterInternal
{
    /// <summary>
    /// Transport id.
    /// </summary>
    public required string TransportId { get; set; }
}

[AutoMetadataFrom(typeof(TransportBaseData), MemberTypes.Property,
    Leading =
        $"public {nameof(TransportBaseData)}(Antelcat.MediasoupSharp.FBS.Transport.{nameof(DumpT)} dump){{",
    Template = "{Name} = dump.{Name};",
    Trailing = "}")]
[AutoMetadataFrom(typeof(TransportBaseData), MemberTypes.Property,
    Leading =
        $"public static implicit operator {nameof(TransportBaseData)}(global::Antelcat.MediasoupSharp.FBS.DirectTransport.{nameof(DumpResponseT)} dump) => new(dump.Base); ")]
[AutoMetadataFrom(typeof(TransportBaseData), MemberTypes.Property,
    Leading =
        $"public static implicit operator global::Antelcat.MediasoupSharp.FBS.Transport.{nameof(DumpT)}({nameof(TransportBaseData)} source) => new (){{",
    Template = "{Name} = source.{Name},",
    Trailing = "};")]
[AutoMetadataFrom(typeof(TransportBaseData), MemberTypes.Property,
    Leading =
        $"public static implicit operator {nameof(TransportBaseData)}(global::Antelcat.MediasoupSharp.FBS.Transport.{nameof(DumpT)} dump) => new(dump);")]
public partial class TransportBaseData
{
    /// <summary>
    /// SCTP parameters.
    /// </summary>
    public SctpParametersT? SctpParameters { get; set; }

    /// <summary>
    /// Sctp state.
    /// </summary>
    public SctpState? SctpState { get; set; }
}

[AutoExtractInterface(NamingTemplate = nameof(ITransport))]
public abstract class TransportImpl<TTransportAppData, TEvents, TObserver> 
    : EnhancedEventEmitter<TEvents>, 
        ITransport<
            TTransportAppData, 
            TEvents, 
            TObserver
        >
    where TTransportAppData : new()
    where TEvents : TransportEvents
    where TObserver : TransportObserver
{
    /// <summary>
    /// Logger factory for create logger.
    /// </summary>
    /// <summary>
    /// Logger.
    /// </summary>
    private readonly ILogger logger = new Logger<IWebRtcTransport>();

    /// <summary>
    /// Whether the Transport is closed.
    /// </summary>
    protected bool Closed { get; private set; }

    /// <summary>
    /// Close locker.
    /// </summary>
    protected readonly AsyncReaderWriterLock CloseLock = new(null);

    /// <summary>
    /// Internal data.
    /// </summary>
    protected TransportInternal Internal { get; }

    /// <summary>
    /// Transport id.
    /// </summary>
    public string Id => Internal.TransportId;

    /// <summary>
    /// Transport data.
    /// </summary>
    public virtual TransportBaseData Data { get; }

    /// <summary>
    /// Channel instance.
    /// </summary>
    protected readonly IChannel Channel;

    /// <summary>
    /// App custom data.
    /// </summary>
    public TTransportAppData AppData { get; set; }

    /// <summary>
    /// Method to retrieve Router RTP capabilities.
    /// </summary>
    protected readonly Func<RtpCapabilities> GetRouterRtpCapabilities;

    /// <summary>
    /// Method to retrieve a Producer.
    /// </summary>
    protected readonly Func<string, Task<IProducer?>> GetProducerById;

    /// <summary>
    /// Method to retrieve a DataProducer.
    /// </summary>
    protected readonly Func<string, Task<IDataProducer?>> GetDataProducerById;

    /// <summary>
    /// Producers map.
    /// </summary>
    protected readonly Dictionary<string, IProducer> Producers = new();

    /// <summary>
    /// Producers locker.
    /// </summary>
    protected readonly AsyncAutoResetEvent ProducersLock = new();

    /// <summary>
    /// Consumers map.
    /// </summary>
    protected readonly Dictionary<string, IConsumer> Consumers = new();

    /// <summary>
    /// Consumers locker.
    /// </summary>
    protected readonly AsyncAutoResetEvent ConsumersLock = new();

    /// <summary>
    /// DataProducers map.
    /// </summary>
    protected readonly Dictionary<string, IDataProducer> DataProducers = new();

    /// <summary>
    /// DataProducers locker.
    /// </summary>
    protected readonly AsyncAutoResetEvent DataProducersLock = new();

    /// <summary>
    /// DataConsumers map.
    /// </summary>
    protected readonly Dictionary<string, IDataConsumer> DataConsumers = new();

    /// <summary>
    /// DataConsumers locker.
    /// </summary>
    protected readonly AsyncAutoResetEvent DataConsumersLock = new();

    /// <summary>
    /// RTCP CNAME for Producers.
    /// </summary>
    private string? cnameForProducers;

    /// <summary>
    /// Next MID for Consumers. It's converted into string when used.
    /// </summary>
    private int nextMidForConsumers;

    private readonly object nextMidForConsumersLock = new();

    /// <summary>
    /// Buffer with available SCTP stream ids.
    /// </summary>
    private ushort[]? sctpStreamIds;

    private readonly object sctpStreamIdsLock = new();

    /// <summary>m
    /// Next SCTP stream id.
    /// </summary>
    private int nextSctpStreamId;

    /// <summary>
    /// Observer instance.
    /// </summary>
    public virtual TObserver Observer { get; }

    /// <summary>
    /// <para>Events : <see cref="TransportEvents"/></para>
    /// <para>Observer events : <see cref="TransportObserverEvents"/></para>
    /// </summary>
    protected TransportImpl(TransportConstructorOptions<TTransportAppData> options, TObserver observer)
    {
        Internal                 = options.Internal;
        Data                     = options.Data;
        Channel                  = options.Channel;
        AppData                  = options.AppData ?? new();
        GetRouterRtpCapabilities = options.GetRouterRtpCapabilities;
        GetProducerById          = options.GetProducerById;
        GetDataProducerById      = options.GetDataProducerById;
        Observer                 = observer;
        ProducersLock.Set();
        ConsumersLock.Set();
        DataProducersLock.Set();
        DataConsumersLock.Set();
    }

    /// <summary>
    /// Close the Transport.
    /// </summary>
    public async Task CloseAsync()
    {
        logger.LogDebug("CloseAsync() | TransportId:{TransportId}", Id);

        await using (await CloseLock.WriteLockAsync())
        {
            if (Closed)
            {
                return;
            }

            Closed = true;

            await OnCloseAsync();

            // Remove notification subscriptions.
            //_channel.OnNotification -= OnNotificationHandle;

            // Build Request
            var bufferBuilder = Channel.BufferPool.Get();

            var requestOffset = Antelcat.MediasoupSharp.FBS.Router.CloseTransportRequest.Pack(bufferBuilder,
                new Antelcat.MediasoupSharp.FBS.Router.CloseTransportRequestT
                {
                    TransportId = Internal.TransportId
                });

            // Fire and forget
            Channel.RequestAsync(bufferBuilder, Method.ROUTER_CLOSE_TRANSPORT,
                    Body.Router_CloseTransportRequest,
                    requestOffset.Value,
                    Internal.RouterId
                )
                .ContinueWithOnFaultedHandleLog(logger);

            await CloseIternalAsync(true);

            this.Emit(static x => x.close);

            // Emit observer event.
            Observer.SafeEmit(static x => x.Close);
        }
    }

    protected abstract Task OnCloseAsync();

    /// <summary>
    /// Router was closed.
    /// </summary>
    public async Task RouterClosedAsync()
    {
        logger.LogDebug("RouterClosed() | TransportId:{TransportId}", Id);

        await using (await CloseLock.WriteLockAsync())
        {
            if (Closed)
            {
                return;
            }

            Closed = true;

            await OnRouterClosedAsync();

            // Remove notification subscriptions.
            //_channel.OnNotification -= OnNotificationHandle;

            await CloseIternalAsync(false);

            this.SafeEmit(static x => x.RouterClose);

            // Emit observer event.
            Observer.SafeEmit(static x => x.Close);
        }
    }

    protected abstract Task OnRouterClosedAsync();

    private async Task CloseIternalAsync(bool tellRouter)
    {
        // Close every Producer.
        await ProducersLock.WaitAsync();
        try
        {
            foreach (var producer in Producers.Values)
            {
                await producer.TransportClosedAsync();

                // Must tell the Router.
                this.Emit(static x => x.producerClose, producer);
            }

            Producers.Clear();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "CloseIternalAsync()");
        }
        finally
        {
            ProducersLock.Set();
        }

        // Close every Consumer.
        await ConsumersLock.WaitAsync();
        try
        {
            foreach (var consumer in Consumers.Values)
            {
                await consumer.TransportClosedAsync();
            }

            Consumers.Clear();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "CloseIternalAsync()");
        }
        finally
        {
            ConsumersLock.Set();
        }

        // Close every DataProducer.
        await DataProducersLock.WaitAsync();
        try
        {
            foreach (var dataProducer in DataProducers.Values)
            {
                await dataProducer.TransportClosedAsync();

                // If call by CloseAsync()
                if (tellRouter)
                {
                    // Must tell the Router.
                    this.Emit(static x => x.dataProducerClose, dataProducer);
                }
                // NOTE: No need to tell the Router since it already knows (it has
                // been closed in fact).
            }

            DataProducers.Clear();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "CloseIternalAsync()");
        }
        finally
        {
            DataProducersLock.Set();
        }

        // Close every DataConsumer.
        await DataConsumersLock.WaitAsync();
        try
        {
            foreach (var dataConsumer in DataConsumers.Values)
            {
                await dataConsumer.TransportClosedAsync();
            }

            DataConsumers.Clear();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "CloseIternalAsync()");
        }
        finally
        {
            DataConsumersLock.Set();
        }
    }

    /// <summary>
    /// Listen server was closed (this just happens in WebRtcTransports when their
    /// associated WebRtcServer is closed).
    /// </summary>
    public async Task ListenServerClosedAsync()
    {
        await using (await CloseLock.WriteLockAsync())
        {
            if (Closed)
            {
                return;
            }

            Closed = true;

            logger.LogDebug("ListenServerClosedAsync() | TransportId:{TransportId}", Id);

            // Remove notification subscriptions.
            //_channel.OnNotification -= OnNotificationHandle;

            await CloseIternalAsync(false);

            // Need to emit this event to let the parent Router know since
            // transport.listenServerClosed() is called by the listen server.
            // NOTE: Currently there is just WebRtcServer for WebRtcTransports.
            this.Emit(static x => x.listenServerClose);
            
            this.SafeEmit(static x => x.ListenServerClose);

            // Emit observer event.
            Observer.SafeEmit(static x => x.Close);
        }
    }

    /// <summary>
    /// Dump Transport.
    /// </summary>
    public async Task<object> DumpAsync()
    {
        logger.LogDebug("DumpAsync() | TransportId:{TransportId}", Id);

        await using (await CloseLock.ReadLockAsync())
        {
            if (Closed)
            {
                throw new InvalidStateException("Transport closed");
            }

            return await OnDumpAsync();
        }
    }

    protected abstract Task<object> OnDumpAsync();

    /// <summary>
    /// Get Transport stats.
    /// </summary>
    public async Task<object[]> GetStatsAsync()
    {
        logger.LogDebug("GetStatsAsync() | TransportId:{TransportId}", Id);

        await using (await CloseLock.ReadLockAsync())
        {
            if (Closed)
            {
                throw new InvalidStateException("Transport closed");
            }

            return await OnGetStatsAsync();
        }
    }

    protected abstract Task<object[]> OnGetStatsAsync();

    /// <summary>
    /// Provide the Transport remote parameters.
    /// </summary>
    public async Task ConnectAsync(object parameters)
    {
        logger.LogDebug("ConnectAsync() | TransportId:{TransportId}", Id);

        await using (await CloseLock.ReadLockAsync())
        {
            if (Closed)
            {
                throw new InvalidStateException("Transport closed");
            }

            await OnConnectAsync(parameters);
        }
    }

    protected abstract Task OnConnectAsync(object parameters);

    /// <summary>
    /// Set maximum incoming bitrate for receiving media.
    /// </summary>
    public virtual async Task SetMaxIncomingBitrateAsync(uint bitrate)
    {
        logger.LogDebug("SetMaxIncomingBitrateAsync() | TransportId:{TransportId} Bitrate:{Bitrate}", Id, bitrate);

        await using (await CloseLock.ReadLockAsync())
        {
            if (Closed)
            {
                throw new InvalidStateException("Transport closed");
            }

            // Build Request
            var bufferBuilder = Channel.BufferPool.Get();

            var setMaxIncomingBitrateRequest = new SetMaxIncomingBitrateRequestT
            {
                MaxIncomingBitrate = bitrate
            };

            var setMaxIncomingBitrateRequestOffset =
                SetMaxIncomingBitrateRequest.Pack(bufferBuilder, setMaxIncomingBitrateRequest);

            // Fire and forget
            Channel.RequestAsync(bufferBuilder, Method.TRANSPORT_SET_MAX_INCOMING_BITRATE,
                Body.Transport_SetMaxIncomingBitrateRequest,
                setMaxIncomingBitrateRequestOffset.Value,
                Internal.TransportId
            ).ContinueWithOnFaultedHandleLog(logger);
        }
    }

    /// <summary>
    /// Set maximum outgoing bitrate for sending media.
    /// </summary>
    public virtual async Task SetMaxOutgoingBitrateAsync(uint bitrate)
    {
        logger.LogDebug("SetMaxOutgoingBitrateAsync() | TransportId:{TransportId} Bitrate:{Bitrate}", Id, bitrate);

        await using (await CloseLock.ReadLockAsync())
        {
            if (Closed)
            {
                throw new InvalidStateException("Transport closed");
            }

            // Build Request
            var bufferBuilder = Channel.BufferPool.Get();

            var setMaxOutgoingBitrateRequest = new SetMaxOutgoingBitrateRequestT
            {
                MaxOutgoingBitrate = bitrate
            };

            var setMaxOutgoingBitrateRequestOffset =
                SetMaxOutgoingBitrateRequest.Pack(bufferBuilder, setMaxOutgoingBitrateRequest);

            // Fire and forget
            Channel.RequestAsync(bufferBuilder, Method.TRANSPORT_SET_MAX_OUTGOING_BITRATE,
                Body.Transport_SetMaxOutgoingBitrateRequest,
                setMaxOutgoingBitrateRequestOffset.Value,
                Internal.TransportId
            ).ContinueWithOnFaultedHandleLog(logger);
        }
    }

    /// <summary>
    /// Set minimum outgoing bitrate for sending media.
    /// </summary>
    public virtual async Task SetMinOutgoingBitrateAsync(uint bitrate)
    {
        logger.LogDebug("SetMinOutgoingBitrateAsync() | TransportId:{TransportId} Bitrate:{Bitrate}", Id, bitrate);

        await using (await CloseLock.ReadLockAsync())
        {
            if (Closed)
            {
                throw new InvalidStateException("Transport closed");
            }

            // Build Request
            var bufferBuilder = Channel.BufferPool.Get();

            var setMinOutgoingBitrateRequest = new SetMinOutgoingBitrateRequestT
            {
                MinOutgoingBitrate = bitrate
            };

            var setMinOutgoingBitrateRequestOffset =
                SetMinOutgoingBitrateRequest.Pack(bufferBuilder, setMinOutgoingBitrateRequest);

            // Fire and forget
            Channel.RequestAsync(bufferBuilder, Method.TRANSPORT_SET_MIN_OUTGOING_BITRATE,
                Body.Transport_SetMinOutgoingBitrateRequest,
                setMinOutgoingBitrateRequestOffset.Value,
                Internal.TransportId
            ).ContinueWithOnFaultedHandleLog(logger);
        }
    }

    /// <summary>
    /// Create a Producer.
    /// </summary>
    public virtual async Task<ProducerImpl<TProducerAppData>> ProduceAsync<TProducerAppData>(
        ProducerOptions<TProducerAppData> producerOptions)
        where TProducerAppData : new()
    {
        logger.LogDebug("ProduceAsync() | TransportId:{TransportId}", Id);

        if (!producerOptions.Id.IsNullOrWhiteSpace() && Producers.ContainsKey(producerOptions.Id))
        {
            throw new Exception($"a Producer with same id \"{producerOptions.Id}\" already exists");
        }

        await using (await CloseLock.ReadLockAsync())
        {
            if (Closed)
            {
                throw new InvalidStateException("Transport closed");
            }

            // This may throw.
            Ortc.ValidateRtpParameters(producerOptions.RtpParameters);

            // If missing or empty encodings, add one.
            // 在 mediasoup-worker 中，要求 Encodings 至少要有一个元素。
            if (producerOptions.RtpParameters.Encodings.IsNullOrEmpty())
            {
                producerOptions.RtpParameters.Encodings = [new()];
            }

            // Don't do this in PipeTransports since there we must keep CNAME value in
            // each Producer.
            if (this is not IPipeTransport)
            {
                // If CNAME is given and we don't have yet a CNAME for Producers in this
                // Transport, take it.
                if (cnameForProducers.IsNullOrWhiteSpace()
                    && producerOptions.RtpParameters.Rtcp.Cname.IsNullOrWhiteSpace() == false)
                {
                    cnameForProducers = producerOptions.RtpParameters.Rtcp.Cname;
                }
                // Otherwise if we don't have yet a CNAME for Producers and the RTP parameters
                // do not include CNAME, create a random one.
                else if (cnameForProducers.IsNullOrWhiteSpace())
                {
                    cnameForProducers = Guid.NewGuid().ToString()[..8];
                }

                // Override Producer's CNAME.
                // 对 RtcpParameters 序列化时，CNAME 和 ReducedSize 为 null 会忽略，因为客户端库对其有校验。
                producerOptions.RtpParameters.Rtcp       ??= new();
                producerOptions.RtpParameters.Rtcp.Cname =   cnameForProducers;
            }

            var routerRtpCapabilities = GetRouterRtpCapabilities();

            // This may throw.
            var rtpMapping =
                Ortc.GetProducerRtpParametersMapping(producerOptions.RtpParameters, routerRtpCapabilities);

            // This may throw.
            var consumableRtpParameters = Ortc.GetConsumableRtpParameters(
                producerOptions.Kind,
                producerOptions.RtpParameters,
                routerRtpCapabilities,
                rtpMapping
            );

            var producerId = producerOptions.Id.NullOrWhiteSpaceThen(Guid.NewGuid().ToString());

            // Build Request
            var bufferBuilder = Channel.BufferPool.Get();

            var produceRequest = new ProduceRequestT
            {
                ProducerId           = producerId,
                Kind                 = producerOptions.Kind,
                RtpParameters        = producerOptions.RtpParameters.SerializeRtpParameters(),
                RtpMapping           = rtpMapping,
                KeyFrameRequestDelay = producerOptions.KeyFrameRequestDelay,
                Paused               = producerOptions.Paused
            };

            var produceRequestOffset = ProduceRequest.Pack(bufferBuilder, produceRequest);

            var response = await Channel.RequestAsync(bufferBuilder, Method.TRANSPORT_PRODUCE,
                Body.Transport_ProduceRequest,
                produceRequestOffset.Value,
                Internal.TransportId);

            var data = response.NotNull().BodyAsTransport_ProduceResponse().UnPack();

            var producerData = new ProducerData
            {
                Kind                    = producerOptions.Kind,
                RtpParameters           = producerOptions.RtpParameters,
                Type                    = data.Type,
                ConsumableRtpParameters = consumableRtpParameters
            };

            var producer = new ProducerImpl<TProducerAppData>(
                new ProducerInternal
                {
                    RouterId    = Internal.RouterId,
                    TransportId = Internal.TransportId,
                    ProducerId  = producerId
                },
                producerData,
                Channel,
                producerOptions.AppData,
                producerOptions.Paused
            );

            producer.On(static x => x.close,
                async _ =>
                {
                    await ProducersLock.WaitAsync();
                    try
                    {
                        Producers.Remove(producer.Id);
                        this.Emit(static x => x.producerClose, producer);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "@close");
                    }
                    finally
                    {
                        ProducersLock.Set();
                    }
                }
            );

            await ProducersLock.WaitAsync();
            try
            {
                Producers[producer.Id] = producer;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "ProduceAsync()");
            }
            finally
            {
                ProducersLock.Set();
            }

            this.Emit(static x => x.newProducer, producer);

            // Emit observer event.
            Observer.SafeEmit(static x => x.NewProducer, producer);

            return producer;
        }
    }

    /// <summary>
    /// Create a Consumer.
    /// </summary>
    public virtual async Task<ConsumerImpl<TConsumeAppData>> ConsumeAsync<TConsumeAppData>(
        ConsumerOptions<TConsumeAppData> consumerOptions)
        where TConsumeAppData : new()
    {
        logger.LogDebug("ConsumeAsync() | TransportId:{TransportId}", Id);

        if (consumerOptions.ProducerId.IsNullOrWhiteSpace())
        {
            throw new ArgumentException($"{nameof(consumerOptions.ProducerId)} can't be null or white space.");
        }

        if (consumerOptions.RtpCapabilities == null)
        {
            throw new ArgumentException($"{nameof(consumerOptions.RtpCapabilities)} can't be null or white space.");
        }

        // Don't use `consumerOptions.Mid?.Length == 0`
        if (consumerOptions.Mid is { Length: 0 })
        {
            throw new ArgumentException($"{nameof(consumerOptions.Mid)} can't be null or white space.");
        }

        await using (await CloseLock.ReadLockAsync())
        {
            if (Closed)
            {
                throw new InvalidStateException("Transport closed");
            }

            // This may throw.
            Ortc.ValidateRtpCapabilities(consumerOptions.RtpCapabilities);

            var producer = await GetProducerById(consumerOptions.ProducerId) ??
                           throw new NullReferenceException($"Producer with id {consumerOptions.ProducerId} not found");

            // This may throw.
            var rtpParameters = Ortc.GetConsumerRtpParameters(
                producer.Data.ConsumableRtpParameters,
                consumerOptions.RtpCapabilities,
                consumerOptions.Pipe
            );

            if (!consumerOptions.Pipe)
            {
                if (consumerOptions.Mid != null)
                {
                    rtpParameters.Mid = consumerOptions.Mid;
                }
                else
                {
                    lock (nextMidForConsumersLock)
                    {
                        // Set MID.
                        rtpParameters.Mid = nextMidForConsumers++.ToString();

                        // We use up to 8 bytes for MID (string).
                        if (nextMidForConsumers == 100_000_000)
                        {
                            logger.LogDebug("ConsumeAsync() | Reaching max MID value {NextMidForConsumers}",
                                nextMidForConsumers);
                            nextMidForConsumers = 0;
                        }
                    }
                }
            }

            var consumerId = Guid.NewGuid().ToString();

            // Build Request
            var bufferBuilder = Channel.BufferPool.Get();

            var consumeRequest = new ConsumeRequestT
            {
                ConsumerId             = consumerId,
                ProducerId             = consumerOptions.ProducerId,
                Kind                   = producer.Data.Kind,
                RtpParameters          = rtpParameters.SerializeRtpParameters(),
                Type                   = consumerOptions.Pipe ? Antelcat.MediasoupSharp.FBS.RtpParameters.Type.PIPE : producer.Data.Type,
                ConsumableRtpEncodings = producer.Data.ConsumableRtpParameters.Encodings,
                Paused                 = consumerOptions.Paused,
                PreferredLayers        = consumerOptions.PreferredLayers,
                IgnoreDtx              = consumerOptions.IgnoreDtx
            };

            var consumeRequestOffset = ConsumeRequest.Pack(bufferBuilder, consumeRequest);

            var response = await Channel.RequestAsync(bufferBuilder, Method.TRANSPORT_CONSUME,
                Body.Transport_ConsumeRequest,
                consumeRequestOffset.Value,
                Internal.TransportId);

            var data = response.NotNull().BodyAsTransport_ConsumeResponse().UnPack();

            var consumerData = new ConsumerData
            {
                ProducerId    = consumerOptions.ProducerId,
                Kind          = producer.Data.Kind,
                RtpParameters = rtpParameters,
                Type          = producer.Data.Type
            };

            var consumer = new ConsumerImpl<TConsumeAppData>(
                new ConsumerInternal
                {
                    RouterId    = Internal.RouterId,
                    TransportId = Internal.TransportId,
                    ConsumerId  = consumerId
                },
                consumerData,
                Channel,
                consumerOptions.AppData,
                data.Paused,
                data.ProducerPaused,
                data.Score,
                data.PreferredLayers
            );

            consumer.On(static x => x.close,
                async _ =>
                {
                    await ConsumersLock.WaitAsync();
                    try
                    {
                        Consumers.Remove(consumer.Id);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "@close");
                    }
                    finally
                    {
                        ConsumersLock.Set();
                    }
                }
            );
            consumer.On(static x => x.producerClose,
                async _ =>
                {
                    await ConsumersLock.WaitAsync();
                    try
                    {
                        Consumers.Remove(consumer.Id);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "@producerclose");
                    }
                    finally
                    {
                        ConsumersLock.Set();
                    }
                }
            );

            await ConsumersLock.WaitAsync();
            try
            {
                Consumers[consumer.Id] = consumer;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "ConsumeAsync()");
            }
            finally
            {
                ConsumersLock.Set();
            }

            // Emit observer event.
            Observer.SafeEmit(static x => x.NewConsumer, consumer);

            return consumer;
        }
    }

    /// <summary>
    /// Create a DataProducer.
    /// </summary>
    public async Task<DataProducerImpl<TDataProducerAppData>> ProduceDataAsync<TDataProducerAppData>(
        DataProducerOptions<TDataProducerAppData> dataProducerOptions)
        where TDataProducerAppData : new()
    {
        logger.LogDebug("ProduceDataAsync() | TransportId:{TransportId}", Id);

        if (!dataProducerOptions.Id.IsNullOrWhiteSpace() && DataProducers.ContainsKey(dataProducerOptions.Id))
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

            Antelcat.MediasoupSharp.FBS.DataProducer.Type type;
            // If this is not a DirectTransport, sctpStreamParameters are required.
            if (this is not IDirectTransport)
            {
                type = Antelcat.MediasoupSharp.FBS.DataProducer.Type.SCTP;

                // This may throw.
                Ortc.ValidateSctpStreamParameters(dataProducerOptions.SctpStreamParameters.NotNull());
            }
            // If this is a DirectTransport, sctpStreamParameters must not be given.
            else
            {
                type = Antelcat.MediasoupSharp.FBS.DataProducer.Type.DIRECT;

                if (dataProducerOptions.SctpStreamParameters != null)
                {
                    logger.LogWarning(
                        "ProduceDataAsync() | TransportId:{TransportId} sctpStreamParameters are ignored when producing data on a DirectTransport",
                        Id);
                }
            }

            var dataProducerId = dataProducerOptions.Id.NullOrWhiteSpaceThen(Guid.NewGuid().ToString());

            // Build Request
            var bufferBuilder = Channel.BufferPool.Get();

            var dataProduceRequest = new ProduceDataRequestT
            {
                DataProducerId       = dataProducerId,
                Type                 = type,
                SctpStreamParameters = dataProducerOptions.SctpStreamParameters,
                Protocol             = dataProducerOptions.Protocol,
                Label                = dataProducerOptions.Label,
                Paused               = dataProducerOptions.Paused
            };

            var dataProduceRequestOffset = ProduceDataRequest.Pack(bufferBuilder, dataProduceRequest);

            var response = await Channel.RequestAsync(bufferBuilder, Method.TRANSPORT_PRODUCE_DATA,
                Body.Transport_ProduceDataRequest,
                dataProduceRequestOffset.Value,
                Internal.TransportId);

            var data = response.NotNull().BodyAsDataProducer_DumpResponse().UnPack();

            var dataProducerData = new DataProducerData
            {
                Type                 = data.Type,
                SctpStreamParameters = data.SctpStreamParameters,
                Label                = data.Label.NotNull(),
                Protocol             = data.Protocol.NotNull()
            };

            var dataProducer = new DataProducerImpl<TDataProducerAppData>(
                new DataProducerInternal
                {
                    RouterId       = Internal.RouterId,
                    TransportId    = Internal.TransportId,
                    DataProducerId = dataProducerId
                },
                dataProducerData,
                Channel,
                dataProducerOptions.Paused,
                dataProducerOptions.AppData
            );

            dataProducer.On(static x => x.close,
                async _ =>
                {
                    await DataProducersLock.WaitAsync();
                    try
                    {
                        DataProducers.Remove(dataProducer.Id);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "@close");
                    }
                    finally
                    {
                        DataProducersLock.Set();
                    }

                    this.Emit(static x => x.dataProducerClose, dataProducer);
                }
            );

            await DataProducersLock.WaitAsync();
            try
            {
                DataProducers[dataProducer.Id] = dataProducer;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "ProduceDataAsync()");
            }
            finally
            {
                DataProducersLock.Set();
            }

            this.Emit(static x => x.newDataProducer, dataProducer);

            // Emit observer event.
            Observer.SafeEmit(static x => x.NewDataProducer, dataProducer);

            return dataProducer;
        }
    }

    /// <summary>
    /// Create a DataConsumer.
    /// </summary>
    public async Task<DataConsumerImpl<TDataConsumerAppData>> ConsumeDataAsync<TDataConsumerAppData>(
        DataConsumerOptions<TDataConsumerAppData> dataConsumerOptions)
        where TDataConsumerAppData : new()
    {
        logger.LogDebug("ConsumeDataAsync() | TransportId:{TransportId}", Id);

        if (dataConsumerOptions.DataProducerId.IsNullOrWhiteSpace())
        {
            throw new Exception("Missing dataProducerId");
        }

        var dataProducer = await GetDataProducerById(dataConsumerOptions.DataProducerId)
                           ?? throw new Exception(
                               $"DataProducer with id {dataConsumerOptions.DataProducerId} not found");

        await using (await CloseLock.ReadLockAsync())
        {
            if (Closed)
            {
                throw new InvalidStateException("Transport closed");
            }

            Antelcat.MediasoupSharp.FBS.DataProducer.Type  type;
            SctpStreamParametersT? sctpStreamParameters = null;
            ushort?                sctpStreamId         = null;

            // If this is not a DirectTransport, use sctpStreamParameters from the
            // DataProducer (if type 'sctp') unless they are given in method parameters.
            if (this is not IDirectTransport)
            {
                type = Antelcat.MediasoupSharp.FBS.DataProducer.Type.SCTP;

                sctpStreamParameters =
                    dataProducer.Data.SctpStreamParameters?.DeepClone() ?? new SctpStreamParametersT();

                if (dataConsumerOptions.Ordered is { } ordered)
                {
                    sctpStreamParameters.Ordered = ordered;
                }

                if (dataConsumerOptions.MaxPacketLifeTime is { } maxPacketLifeTime)
                {
                    sctpStreamParameters.MaxPacketLifeTime = (ushort)maxPacketLifeTime;
                }

                if (dataConsumerOptions.MaxRetransmits is { } maxRetransmits)
                {
                    sctpStreamParameters.MaxRetransmits = (ushort)maxRetransmits;
                }

                // This may throw.
                lock (sctpStreamIdsLock)
                {
                    sctpStreamId = GetNextSctpStreamId();

                    if (sctpStreamIds == null || sctpStreamId > sctpStreamIds.Length - 1)
                    {
                        throw new IndexOutOfRangeException(nameof(sctpStreamIds));
                    }

                    sctpStreamIds[sctpStreamId.Value] = 1;
                    sctpStreamParameters.StreamId     = sctpStreamId.Value;
                }
            }
            // If this is a DirectTransport, sctpStreamParameters must not be used.
            else
            {
                type = Antelcat.MediasoupSharp.FBS.DataProducer.Type.DIRECT;

                if (
                    dataConsumerOptions.Ordered.HasValue
                    || dataConsumerOptions.MaxPacketLifeTime.HasValue
                    || dataConsumerOptions.MaxRetransmits.HasValue
                )
                {
                    logger.LogWarning(
                        "ConsumeDataAsync() | Ordered, maxPacketLifeTime and maxRetransmits are ignored when consuming data on a DirectTransport"
                    );
                }
            }

            var dataConsumerId = Guid.NewGuid().ToString();

            // Build Request
            var bufferBuilder = Channel.BufferPool.Get();

            var consumeDataRequest = new ConsumeDataRequestT
            {
                DataConsumerId       = dataConsumerId,
                DataProducerId       = dataConsumerOptions.DataProducerId,
                Type                 = type,
                SctpStreamParameters = sctpStreamParameters,
                Label                = dataProducer.Data.Label,
                Protocol             = dataProducer.Data.Protocol,
                Paused               = dataConsumerOptions.Paused,
                Subchannels          = dataConsumerOptions.Subchannels
            };

            var consumeDataRequestOffset = ConsumeDataRequest.Pack(bufferBuilder, consumeDataRequest);

            var response = await Channel.RequestAsync(bufferBuilder, Method.TRANSPORT_CONSUME_DATA,
                Body.Transport_ConsumeDataRequest,
                consumeDataRequestOffset.Value,
                Internal.TransportId);

            var data = response.NotNull().BodyAsDataConsumer_DumpResponse().UnPack();

            var dataConsumerData = new DataConsumerData
            {
                DataProducerId             = data.DataProducerId,
                Type                       = data.Type,
                SctpStreamParameters       = data.SctpStreamParameters,
                Label                      = data.Label,
                Protocol                   = data.Protocol,
                BufferedAmountLowThreshold = data.BufferedAmountLowThreshold
            };

            var dataConsumer = new DataConsumerImpl<TDataConsumerAppData>(
                new DataConsumerInternal
                {
                    RouterId       = Internal.RouterId,
                    TransportId    = Internal.TransportId,
                    DataConsumerId = dataConsumerId
                },
                dataConsumerData,
                Channel,
                data.Paused,
                data.DataProducerPaused,
                data.Subchannels,
                dataConsumerOptions.AppData
            );

            dataConsumer.On(static x => x.close,
                async _ =>
                {
                    await DataConsumersLock.WaitAsync();
                    try
                    {
                        DataConsumers.Remove(dataConsumer.Id);
                        lock (sctpStreamIdsLock)
                        {
                            if (sctpStreamIds != null && sctpStreamId.HasValue)
                            {
                                sctpStreamIds[sctpStreamId.Value] = 0;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "@close");
                    }
                    finally
                    {
                        DataConsumersLock.Set();
                    }
                }
            );

            dataConsumer.On(static x => x.dataProducerClose,
                async _ =>
                {
                    await DataConsumersLock.WaitAsync();
                    try
                    {
                        DataConsumers.Remove(dataConsumer.Id);
                        lock (sctpStreamIdsLock)
                        {
                            if (sctpStreamIds != null && sctpStreamId.HasValue)
                            {
                                sctpStreamIds[sctpStreamId.Value] = 0;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "@dataproducerclose");
                    }
                    finally
                    {
                        DataConsumersLock.Set();
                    }
                }
            );

            await DataConsumersLock.WaitAsync();
            try
            {
                DataConsumers[dataConsumer.Id] = dataConsumer;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "ConsumeDataAsync()");
            }
            finally
            {
                DataConsumersLock.Set();
            }

            // Emit observer event.
            Observer.Emit(static x => x.NewDataConsumer, dataConsumer);

            return dataConsumer;
        }
    }

    /// <summary>
    /// Enable 'trace' event.
    /// </summary>
    public async Task EnableTraceEventAsync(List<Antelcat.MediasoupSharp.FBS.Transport.TraceEventType> types)
    {
        logger.LogDebug("EnableTraceEventAsync() | Transport:{TransportId}", Id);

        await using (await CloseLock.ReadLockAsync())
        {
            if (Closed)
            {
                throw new InvalidStateException("Transport closed");
            }

            // Build Request
            var bufferBuilder = Channel.BufferPool.Get();

            var enableTraceEventRequest = new EnableTraceEventRequestT
            {
                Events = types ?? []
            };

            var requestOffset = EnableTraceEventRequest.Pack(bufferBuilder, enableTraceEventRequest);

            // Fire and forget
            Channel.RequestAsync(bufferBuilder, Method.TRANSPORT_ENABLE_TRACE_EVENT,
                    Body.Transport_EnableTraceEventRequest,
                    requestOffset.Value,
                    Internal.TransportId)
                .ContinueWithOnFaultedHandleLog(logger);
        }
    }

    #region Private Methods

    private ushort GetNextSctpStreamId()
    {
        if (Data.SctpParameters == null)
        {
            throw new NullReferenceException("Missing data.sctpParameters.MIS");
        }

        var numStreams = Data.SctpParameters.Mis;

        sctpStreamIds ??= new ushort[numStreams];

        for (var idx = 0; idx < sctpStreamIds.Length; ++idx)
        {
            var sctpStreamId = (ushort)((nextSctpStreamId + idx) % sctpStreamIds.Length);

            if (sctpStreamIds[sctpStreamId] == 0)
            {
                nextSctpStreamId = sctpStreamId + 1;
                return sctpStreamId;
            }
        }

        throw new IndexOutOfRangeException("No sctpStreamId available");
    }

    #endregion Private Methods
}