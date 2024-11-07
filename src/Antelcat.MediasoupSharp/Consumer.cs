﻿using Antelcat.AutoGen.ComponentModel.Diagnostic;
using Antelcat.MediasoupSharp.Internals.Extensions;
using FBS.Consumer;
using FBS.Notification;
using FBS.Request;
using FBS.RtpParameters;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Threading;

namespace Antelcat.MediasoupSharp;

using ConsumerLayers = FBS.Consumer.ConsumerLayersT;
using ConsumerObserver = IEnhancedEventEmitter<ConsumerObserverEvents>;
using ConsumerScore = FBS.Consumer.ConsumerScoreT;
using ConsumerTraceEventType = FBS.Consumer.TraceEventType;

public class ConsumerOptions<TConsumerAppData>
{
    /// <summary>
    /// The id of the Producer to consume.
    /// </summary>
    public required string ProducerId { get; set; }
    
    /// <summary>
    /// RTP capabilities of the consuming endpoint.
    /// </summary>
    public required RtpCapabilities RtpCapabilities { get; set; }

    /// <summary>
    /// <para>Whether the Consumer must start in paused mode. Default false.</para>
    /// <para>
    /// When creating a video Consumer, it's recommended to set paused to true,
    /// then transmit the Consumer parameters to the consuming endpoint and, once
    /// the consuming endpoint has created its local side Consumer, unpause the
    /// server side Consumer using the resume() method. This is an optimization
    /// to make it possible for the consuming endpoint to render the video as far
    /// as possible. If the server side Consumer was created with paused: false,
    /// mediasoup will immediately request a key frame to the remote Producer and
    /// such a key frame may reach the consuming endpoint even before it's ready
    /// to consume it, generating “black” video until the device requests a keyframe
    /// by itself.
    /// </para>
    /// </summary>
    public bool Paused { get; set; }

    /// <summary>
    /// The MID for the Consumer. If not specified, a sequentially growing
    /// number will be assigned.
    /// </summary>
    public string? Mid { get; set; }

    /// <summary>
    /// Preferred spatial and temporal layer for simulcast or SVC media sources.
    /// If unset, the highest ones are selected.
    /// </summary>
    public ConsumerLayers? PreferredLayers { get; set; }

    /**
     * Whether this Consumer should enable RTP retransmissions, storing sent RTP
     * and processing the incoming RTCP NACK from the remote Consumer. If not set
     * it's true by default for video codecs and false for audio codecs. If set
     * to true, NACK will be enabled if both endpoints (mediasoup and the remote
     * Consumer) support NACK for this codec. When it comes to audio codecs, just
     * OPUS supports NACK.
     */
    public bool EnableRtx { get; set; }

    /// <summary>
    /// Whether this Consumer should ignore DTX packets (only valid for Opus codec).
    /// If set, DTX packets are not forwarded to the remote Consumer.
    /// </summary>
    public bool IgnoreDtx { get; set; }

    /// <summary>
    /// Whether this Consumer should consume all RTP streams generated by the
    /// Producer.
    /// </summary>
    public bool Pipe { get; set; }
    
    /// <summary>
    /// Custom application data.
    /// </summary>
    public TConsumerAppData? AppData { get; set; }
}


public abstract class ConsumerEvents
{
    public          object?            TransportClose;
    public          object?            ProducerClose;
    public          object?            ProducerPause;
    public          object?            ProducerResume;
    public required ConsumerScore      Score;
    public          ConsumerLayers?    LayersChange;
    public required TraceNotificationT Trace;
    public required byte[]             Rtp;

    public required (string, Exception) ListenerError;

    // Private events.
    internal object? close;
    internal object? producerClose;
}


public abstract class ConsumerObserverEvents
{
    public          object?             Close;
    public          object?             Pause;
    public          object?             Resume;
    public required ConsumerScore       Score;
    public          ConsumerLayers?     LayersChange;
    public          TraceNotificationT? Trace;
}


public class ConsumerData
{
    /// <summary>
    /// Associated Producer id.
    /// </summary>
    public required string ProducerId { get; init; }

    /// <summary>
    /// Media kind.
    /// </summary>
    public MediaKind Kind { get; set; }

    /// <summary>
    /// RTP parameters.
    /// </summary>
    public required RtpParameters RtpParameters { get; set; }

    /// <summary>
    /// Consumer type.
    /// </summary>
    public FBS.RtpParameters.Type Type { get; set; }
}

public class ConsumerInternal : TransportInternal
{
    /// <summary>
    /// Consumer id.
    /// </summary>
    public required string ConsumerId { get; set; }
}

[AutoExtractInterface]
public class Consumer<TConsumerAppData> : EnhancedEventEmitter<ConsumerEvents> , IConsumer
    where TConsumerAppData : new()
{
    /// <summary>
    /// Logger.
    /// </summary>
    private readonly ILogger<Consumer<TConsumerAppData>> logger 
        = new Logger<Consumer<TConsumerAppData>>();

    /// <summary>
    /// Whether the Consumer is closed.
    /// </summary>
    private bool closed;

    private readonly AsyncReaderWriterLock closeLock = new();

    /// <summary>
    /// Paused flag.
    /// </summary>
    private bool paused;

    private readonly AsyncAutoResetEvent pauseLock = new();

    /// <summary>
    /// Internal data.
    /// </summary>
    private readonly ConsumerInternal @internal;

    /// <summary>
    /// Consumer id.
    /// </summary>
    public string Id => @internal.ConsumerId;

    /// <summary>
    /// Consumer data.
    /// </summary>
    public ConsumerData Data { get; set; }

    /// <summary>
    /// Producer id.
    /// </summary>
    public string ProducerId => Data.ProducerId;

    /// <summary>
    /// Channel instance.
    /// </summary>
    private readonly IChannel channel;

    /// <summary>
    /// App custom data.
    /// </summary>
    public TConsumerAppData AppData { get; }

    /// <summary>
    /// [扩展]Source.
    /// </summary>
    public string? Source { get; set; }

    /// <summary>
    /// Whether the associate Producer is paused.
    /// </summary>
    public bool ProducerPaused { get; private set; }

    /// <summary>
    /// Current priority.
    /// </summary>
    public int Priority { get; private set; } = 1;

    /// <summary>
    /// Current score.
    /// </summary>
    public ConsumerScore? Score;

    /// <summary>
    /// Preferred layers.
    /// </summary>
    public ConsumerLayers? PreferredLayers { get; private set; }

    /// <summary>
    /// Current layers.
    /// </summary>
    public ConsumerLayers? CurrentLayers { get; private set; }

    /// <summary>
    /// Observer instance.
    /// </summary>
    public ConsumerObserver Observer { get; } = new EnhancedEventEmitter<ConsumerObserverEvents>();

    /// <summary>
    /// <para>Events:</para>
    /// <para>@emits <see cref="ConsumerEvents.TransportClose"/></para>
    /// <para>@emits <see cref="ConsumerEvents.ProducerClose"/></para>
    /// <para>@emits <see cref="ConsumerEvents.ProducerPause"/></para>
    /// <para>@emits <see cref="ConsumerEvents.ProducerResume"/></para>
    /// <para>@emits <see cref="ConsumerEvents.Score"/> - (score: ConsumerScore)</para>
    /// <para>@emits <see cref="ConsumerEvents.LayersChange"/> - (layers: ConsumerLayers | undefined)</para>
    /// <para>@emits <see cref="ConsumerEvents.Trace"/> - (trace: ConsumerTraceEventData)</para>
    /// <para>@emits <see cref="ConsumerEvents.Rtp"/> - (packet: Buffer)</para>
    /// <para>@emits <see cref="ConsumerEvents.close"/>@</para>
    /// <para>@emits <see cref="ConsumerEvents.producerClose"/>@</para>
    /// <para>Observer events:</para>
    /// <para>@emits <see cref="ConsumerObserverEvents.Close"/></para>
    /// <para>@emits <see cref="ConsumerObserverEvents.Pause"/></para>
    /// <para>@emits <see cref="ConsumerObserverEvents.Resume"/></para>
    /// <para>@emits <see cref="ConsumerObserverEvents.Score"/> - (score: ConsumerScore)</para>
    /// <para>@emits <see cref="ConsumerObserverEvents.LayersChange"/> - (layers: ConsumerLayers | undefined)</para>
    /// <para>@emits <see cref="ConsumerObserverEvents.Trace"/> - (trace: ConsumerTraceEventData)</para>
    /// </summary>
    public Consumer(
        ConsumerInternal @internal,
        ConsumerData data,
        IChannel channel,
        TConsumerAppData? appData,
        bool paused,
        bool producerPaused,
        ConsumerScore? score,
        ConsumerLayers? preferredLayers
    )
    {
        this.@internal  = @internal;
        Data            = data;
        this.channel    = channel;
        AppData         = appData ?? new();
        this.paused     = paused;
        ProducerPaused  = producerPaused;
        Score           = score;
        PreferredLayers = preferredLayers;
        pauseLock.Set();

        HandleWorkerNotifications();
    }

    /// <summary>
    /// Close the Producer.
    /// </summary>
    public async Task CloseAsync()
    {
        logger.LogDebug($"{nameof(CloseAsync)}() | Consumer:{{ConsumerId}}", Id);

        await using(await closeLock.WriteLockAsync())
        {
            if(closed)
            {
                return;
            }

            closed = true;

            // Remove notification subscriptions.
            channel.OnNotification -= OnNotificationHandle;

            // Build Request
            var bufferBuilder = channel.BufferPool.Get();

            var requestOffset = FBS.Transport.CloseConsumerRequest.Pack(bufferBuilder, new FBS.Transport.CloseConsumerRequestT
            {
                ConsumerId = @internal.ConsumerId
            });

            // Fire and forget
            channel.RequestAsync(
                    bufferBuilder,
                    Method.TRANSPORT_CLOSE_CONSUMER,
                    FBS.Request.Body.Transport_CloseConsumerRequest,
                    requestOffset.Value,
                    @internal.TransportId
                )
                .ContinueWithOnFaultedHandleLog(logger);

            this.Emit(static x=>x.close);

            // Emit observer event.
            Observer.Emit(static x => x.Close);
        }
    }

    /// <summary>
    /// Transport was closed.
    /// </summary>
    public async Task TransportClosedAsync()
    {
        logger.LogDebug($"{nameof(TransportClosedAsync)}() | Consumer:{{ConsumerId}}", Id);

        await using(await closeLock.WriteLockAsync())
        {
            if(closed)
            {
                return;
            }

            closed = true;

            // Remove notification subscriptions.
            channel.OnNotification -= OnNotificationHandle;

            this.Emit(static x => x.TransportClose);

            // Emit observer event.
            Observer.Emit(static x=>x.Close);
        }
    }

    /// <summary>
    /// Dump DataProducer.
    /// </summary>
    public async Task<FBS.Consumer.DumpResponseT> DumpAsync()
    {
        logger.LogDebug($"{nameof(DumpAsync)}() | Consumer:{{ConsumerId}}", Id);

        await using(await closeLock.ReadLockAsync())
        {
            if(closed)
            {
                throw new InvalidStateException("Consumer closed");
            }

            var bufferBuilder = channel.BufferPool.Get();
            var response = await channel.RequestAsync(bufferBuilder, Method.CONSUMER_DUMP, null, null, @internal.ConsumerId);
            var data = response.NotNull().BodyAsConsumer_DumpResponse().UnPack();
            return data;
        }
    }

    /// <summary>
    /// Get DataProducer stats.
    /// </summary>
    public async Task<List<FBS.RtpStream.StatsT>> GetStatsAsync()
    {
        logger.LogDebug($"{nameof(GetStatsAsync)}() | Consumer:{{ConsumerId}}", Id);

        await using(await closeLock.ReadLockAsync())
        {
            if(closed)
            {
                throw new InvalidStateException("Consumer closed");
            }

            var bufferBuilder = channel.BufferPool.Get();
            var response = await channel.RequestAsync(bufferBuilder, Method.CONSUMER_GET_STATS, null, null, @internal.ConsumerId);
            var stats = response.NotNull().BodyAsConsumer_GetStatsResponse().UnPack().Stats;
            return stats;
        }
    }

    /// <summary>
    /// Pause the Consumer.
    /// </summary>
    public async Task PauseAsync()
    {
        logger.LogDebug($"{nameof(PauseAsync)}() | Consumer:{{ConsumerId}}", Id);

        await using(await closeLock.ReadLockAsync())
        {
            if(closed)
            {
                throw new InvalidStateException("Consumer closed");
            }

            await pauseLock.WaitAsync();
            try
            {
                var wasPaused = paused || ProducerPaused;

                var bufferBuilder = channel.BufferPool.Get();

                // Fire and forget
                channel.RequestAsync(bufferBuilder, Method.CONSUMER_PAUSE, null, null, @internal.ConsumerId)
                    .ContinueWithOnFaultedHandleLog(logger);

                paused = true;

                // Emit observer event.
                if(!wasPaused)
                {
                    Observer.Emit(static x=>x.Pause);
                }
            }
            catch(Exception ex)
            {
                logger.LogError(ex, "PauseAsync()");
            }
            finally
            {
                pauseLock.Set();
            }
        }
    }

    /// <summary>
    /// Resume the Consumer.
    /// </summary>
    public async Task ResumeAsync()
    {
        logger.LogDebug($"{nameof(ResumeAsync)}() | Consumer:{{ConsumerId}}", Id);

        await using(await closeLock.ReadLockAsync())
        {
            if(closed)
            {
                throw new InvalidStateException("Consumer closed");
            }

            await pauseLock.WaitAsync();
            try
            {
                var wasPaused = paused || ProducerPaused;

                var bufferBuilder = channel.BufferPool.Get();

                // Fire and forget
                channel.RequestAsync(bufferBuilder, Method.CONSUMER_RESUME, null, null, @internal.ConsumerId)
                    .ContinueWithOnFaultedHandleLog(logger);

                paused = false;

                // Emit observer event.
                if(wasPaused && !ProducerPaused)
                {
                    Observer.Emit(static x=>x.Resume);
                }
            }
            catch(Exception ex)
            {
                logger.LogError(ex, "ResumeAsync()");
            }
            finally
            {
                pauseLock.Set();
            }
        }
    }

    /// <summary>
    /// Set preferred video layers.
    /// </summary>
    public async Task SetPreferredLayersAsync(FBS.Consumer.SetPreferredLayersRequestT setPreferredLayersRequest)
    {
        logger.LogDebug($"{nameof(SetPreferredLayersAsync)}() | Consumer:{{ConsumerId}}", Id);

        await using(await closeLock.ReadLockAsync())
        {
            if(closed)
            {
                throw new InvalidStateException("Consumer closed");
            }

            // Build Request
            var bufferBuilder = channel.BufferPool.Get();

            var setPreferredLayersRequestOffset = SetPreferredLayersRequest.Pack(bufferBuilder, setPreferredLayersRequest);

            var response = await channel.RequestAsync(
                bufferBuilder,
                Method.CONSUMER_SET_PREFERRED_LAYERS,
                FBS.Request.Body.Consumer_SetPreferredLayersRequest,
                setPreferredLayersRequestOffset.Value,
                @internal.ConsumerId);
            var preferredLayers = response?.BodyAsConsumer_SetPreferredLayersResponse().UnPack().PreferredLayers;

            PreferredLayers = preferredLayers;
        }
    }

    /// <summary>
    /// Set priority.
    /// </summary>
    public async Task SetPriorityAsync(FBS.Consumer.SetPriorityRequestT setPriorityRequest)
    {
        logger.LogDebug($"{nameof(SetPriorityAsync)}() | Consumer:{{ConsumerId}}", Id);

        await using(await closeLock.ReadLockAsync())
        {
            if(closed)
            {
                throw new InvalidStateException("Consumer closed");
            }

            var bufferBuilder = channel.BufferPool.Get();

            var setPriorityRequestOffset = SetPriorityRequest.Pack(bufferBuilder, setPriorityRequest);

            var response = await channel.RequestAsync(
                bufferBuilder,
                Method.CONSUMER_SET_PRIORITY,
                FBS.Request.Body.Consumer_SetPriorityRequest,
                setPriorityRequestOffset.Value,
                @internal.ConsumerId);

            var priorityResponse = response.NotNull().BodyAsConsumer_SetPriorityResponse().UnPack().Priority;

            Priority = priorityResponse;
        }
    }

    /// <summary>
    /// Unset priority.
    /// </summary>
    public Task UnsetPriorityAsync()
    {
        logger.LogDebug($"{nameof(UnsetPriorityAsync)}() | Consumer:{{ConsumerId}}", Id);

        return SetPriorityAsync(new SetPriorityRequestT
        {
            Priority = 1
        });
    }

    /// <summary>
    /// Request a key frame to the Producer.
    /// </summary>
    public async Task RequestKeyFrameAsync()
    {
        logger.LogDebug($"{nameof(RequestKeyFrameAsync)}() | Consumer:{{ConsumerId}}", Id);

        await using(await closeLock.ReadLockAsync())
        {
            if(closed)
            {
                throw new InvalidStateException("Consumer closed");
            }

            // Build Request
            var bufferBuilder = channel.BufferPool.Get();

            await channel.RequestAsync(bufferBuilder, Method.CONSUMER_REQUEST_KEY_FRAME,
                null,
                null,
                @internal.ConsumerId);
        }
    }

    /// <summary>
    /// Enable 'trace' event.
    /// </summary>
    public async Task EnableTraceEventAsync(List<FBS.Consumer.TraceEventType> types)
    {
        logger.LogDebug($"{nameof(EnableTraceEventAsync)}() | Consumer:{{ConsumerId}}", Id);

        await using(await closeLock.ReadLockAsync())
        {
            if(closed)
            {
                throw new InvalidStateException("Consumer closed");
            }

            // Build Request
            var bufferBuilder = channel.BufferPool.Get();

            var request = new EnableTraceEventRequestT
            {
                Events = types ?? []
            };

            var requestOffset = EnableTraceEventRequest.Pack(bufferBuilder, request);

            // Fire and forget
            channel.RequestAsync(
                    bufferBuilder,
                    Method.CONSUMER_ENABLE_TRACE_EVENT,
                    FBS.Request.Body.Consumer_EnableTraceEventRequest,
                    requestOffset.Value,
                    @internal.ConsumerId)
                .ContinueWithOnFaultedHandleLog(logger);
        }
    }

    #region Event Handlers

    private void HandleWorkerNotifications()
    {
        channel.OnNotification += OnNotificationHandle;
    }

#pragma warning disable VSTHRD100 // Avoid async void methods
    private async void OnNotificationHandle(string handlerId, Event @event, Notification notification)
#pragma warning restore VSTHRD100 // Avoid async void methods
    {
        if(handlerId != Id)
        {
            return;
        }

        switch(@event)
        {
            case Event.CONSUMER_PRODUCER_CLOSE:
            {
                await using(await closeLock.WriteLockAsync())
                {
                    if(closed)
                    {
                        break;
                    }

                    closed = true;

                    // Remove notification subscriptions.
                    channel.OnNotification -= OnNotificationHandle;

                    this.Emit(static x => x.ProducerClose);
                    this.Emit(static x=>x.ProducerClose);

                    // Emit observer event.
                    Observer.Emit(static x=>x.Close);
                }

                break;
            }
            case Event.CONSUMER_PRODUCER_PAUSE:
            {
                if(ProducerPaused)
                {
                    break;
                }

                var wasPaused = paused || ProducerPaused;

                ProducerPaused = true;

                this.Emit(static x => x.ProducerPause);

                // Emit observer event.
                if(!wasPaused)
                {
                    Observer.Emit(static x=>x.Pause);
                }

                break;
            }
            case Event.CONSUMER_PRODUCER_RESUME:
            {
                if(!ProducerPaused)
                {
                    break;
                }

                var wasPaused = paused || ProducerPaused;

                ProducerPaused = false;

                this.Emit(static x=>x.ProducerResume);

                // Emit observer event.
                if(wasPaused && !paused)
                {
                    Observer.Emit(static x=>x.Resume);
                }

                break;
            }
            case Event.CONSUMER_SCORE:
            {
                var scoreNotification = notification.BodyAsConsumer_ScoreNotification();
                var score             = scoreNotification.Score.NotNull().UnPack();
                Score = score;

                this.Emit(static x=>x.Score, Score);

                // Emit observer event.
                Observer.Emit(static x => x.Score, Score);

                break;
            }
            case Event.CONSUMER_LAYERS_CHANGE:
            {
                var layersChangeNotification = notification.BodyAsConsumer_LayersChangeNotification();
                var currentLayers            = layersChangeNotification.Layers?.UnPack();
                CurrentLayers = currentLayers;

                this.Emit(static x => x.LayersChange, CurrentLayers);

                // Emit observer event.
                Observer.Emit(static x => x.LayersChange, CurrentLayers);

                break;
            }
            case Event.CONSUMER_TRACE:
            {
                var traceNotification = notification.BodyAsConsumer_TraceNotification();
                var trace             = traceNotification.UnPack();

                this.Emit(static x => x.Trace, trace);

                // Emit observer event.
                Observer.Emit(static x => x.Trace, trace);

                break;
            }
            default:
            {
                logger.LogError("OnNotificationHandle() | Ignoring unknown event{Event}", @event);
                break;
            }
        }
    }
}

#endregion Event Handlers