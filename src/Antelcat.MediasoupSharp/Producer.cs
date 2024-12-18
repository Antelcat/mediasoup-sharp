﻿using Antelcat.AutoGen.ComponentModel.Diagnostic;
using Antelcat.MediasoupSharp.FBS.Notification;
using Antelcat.MediasoupSharp.FBS.Producer;
using Antelcat.MediasoupSharp.FBS.Request;
using Antelcat.MediasoupSharp.FBS.RtpParameters;
using Antelcat.MediasoupSharp.Internals.Extensions;
using Antelcat.MediasoupSharp.Internals.Utils;
using Google.FlatBuffers;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Threading;
using Type = Antelcat.MediasoupSharp.FBS.RtpParameters.Type;

namespace Antelcat.MediasoupSharp;


public class ProducerInternal : TransportInternal
{
    /// <summary>
    /// Producer id.
    /// </summary>
    public required string ProducerId { get; init; }
}

public class ProducerData
{
    /// <summary>
    /// Media kind.
    /// </summary>
    public MediaKind Kind { get; init; }

    /// <summary>
    /// RTP parameters.
    /// </summary>
    public required RtpParameters RtpParameters { get; init; }

    /// <summary>
    /// Producer type.
    /// </summary>
    public Type Type { get; init; }

    /// <summary>
    /// Consumable RTP parameters.
    /// </summary>
    public required RtpParameters ConsumableRtpParameters { get; init; }
}

[AutoExtractInterface(NamingTemplate = nameof(IProducer))]
public class ProducerImpl<TProducerAppData> 
    : EnhancedEventEmitter<ProducerEvents>, IProducer<TProducerAppData>
    where TProducerAppData : new()
{
    /// <summary>
    /// Logger
    /// </summary>
    private readonly ILogger logger = new Logger<ProducerImpl<TProducerAppData>>();

    /// <summary>
    /// Producer id.
    /// </summary>
    public string Id => @internal.ProducerId;

    /// <summary>
    /// Whether the Producer is closed.
    /// </summary>
    public bool Closed { get; private set; }

    public Antelcat.MediasoupSharp.FBS.RtpParameters.MediaKind Kind => Data.Kind;

    private readonly AsyncReaderWriterLock closeLock = new(null);

    /// <summary>
    /// Paused flag.
    /// </summary>
    public bool Paused { get; private set; }

    private readonly AsyncAutoResetEvent pauseLock = new();

    /// <summary>
    /// Internal data.
    /// </summary>
    private readonly ProducerInternal @internal;

    private readonly bool isCheckConsumer = false;

    private readonly Timer? checkConsumersTimer;

#if DEBUG
    private const int CheckConsumersTimeSeconds = 60 * 60 * 24;
#else
        private const int CheckConsumersTimeSeconds = 10;
#endif

    /// <summary>
    /// Producer data.
    /// </summary>
    public ProducerData Data { get; set; }

    /// <summary>
    /// Channel instance.
    /// </summary>
    private readonly IChannel channel;

    /// <summary>
    /// App custom data.
    /// </summary>
    public TProducerAppData AppData { get; set; }

    /// <summary>
    /// [扩展]Consumers
    /// </summary>
    private readonly Dictionary<string, IConsumer> consumers = new();

    /// <summary>
    /// [扩展]Source.
    /// </summary>
    public string? Source { get; set; }

    /// <summary>
    /// Current score.
    /// </summary>
    public List<ScoreT> Score = [];

    /// <summary>
    /// Observer instance.
    /// </summary>
    public ProducerObserver Observer { get; } = new();

    /// <summary>
    /// <para>Events : <see cref="ProducerEvents"/></para>
    /// <para>Observer events : <see cref="ProducerObserverEvents"/></para>
    /// </summary>
    public ProducerImpl(
        ProducerInternal @internal,
        ProducerData data,
        IChannel channel,
        TProducerAppData? appData,
        bool paused
    )
    {
        this.@internal = @internal;
        Data           = data;
        this.channel   = channel;
        Paused         = paused;
        AppData        = appData ?? new ();
        
        pauseLock.Set();

        if (isCheckConsumer)
        {
            checkConsumersTimer = new Timer(
                CheckConsumers,
                null,
                TimeSpan.FromSeconds(CheckConsumersTimeSeconds),
                TimeSpan.FromMilliseconds(-1)
            );
        }

        HandleWorkerNotifications();
        HandleListenerError();
    }

    /// <summary>
    /// Close the Producer.
    /// </summary>
    public async Task CloseAsync()
    {
        logger.LogDebug("CloseAsync() | Producer:{ProducerId}", Id);

        await using (await closeLock.WriteLockAsync())
        {
            CloseInternal();
        }
    }

    private void CloseInternal()
    {
        if (Closed)
        {
            return;
        }

        Closed = true;

        checkConsumersTimer?.Dispose();

        // Remove notification subscriptions.
        channel.OnNotification -= OnNotificationHandle;

        // Fire and forget
        channel.RequestAsync(bufferBuilder => 
                    Antelcat.MediasoupSharp.FBS.Transport.CloseProducerRequest.Pack(bufferBuilder,
                    new Antelcat.MediasoupSharp.FBS.Transport.CloseProducerRequestT
                    {
                        ProducerId = @internal.ProducerId
                    }).Value, Method.TRANSPORT_CLOSE_CONSUMER,
                Antelcat.MediasoupSharp.FBS.Request.Body.Transport_CloseConsumerRequest,
                @internal.TransportId
            )
            .ContinueWithOnFaultedHandleLog(logger);

        this.Emit(static x => x.close);

        // Emit observer event.
        Observer.SafeEmit(static x=>x.Close);
    }

    /// <summary>
    /// Transport was closed.
    /// </summary>
    public async Task TransportClosedAsync()
    {
        logger.LogDebug("TransportClosedAsync() | Producer:{ProducerId}", Id);

        await using (await closeLock.WriteLockAsync())
        {
            if (Closed) return;

            Closed = true;

            if (checkConsumersTimer != null) await checkConsumersTimer.DisposeAsync();

            // Remove notification subscriptions.
            channel.OnNotification -= OnNotificationHandle;

            this.SafeEmit(static x=>x.TransportClose);

            // Emit observer event.
            Observer.SafeEmit(static x=>x.Close);
        }
    }

    /// <summary>
    /// Dump DataProducer.
    /// </summary>
    public async Task<Antelcat.MediasoupSharp.FBS.Producer.DumpResponseT> DumpAsync()
    {
        logger.LogDebug("DumpAsync() | Producer:{ProducerId}", Id);

        await using (await closeLock.ReadLockAsync())
        {
            if (Closed) throw new InvalidStateException("Producer closed");

            var response =
                await channel.RequestAsync(static _ => null,
                    Method.PRODUCER_DUMP, 
                    null, 
                    @internal.ProducerId);
            var data = response.NotNull().BodyAsProducer_DumpResponse().UnPack();

            return data;
        }
    }

    /// <summary>
    /// Get DataProducer stats.
    /// </summary>
    public async Task<List<Antelcat.MediasoupSharp.FBS.RtpStream.StatsT>> GetStatsAsync()
    {
        logger.LogDebug("GetStatsAsync() | Producer:{ProducerId}", Id);

        await using (await closeLock.ReadLockAsync())
        {
            if (Closed) throw new InvalidStateException("Producer closed");

            var response = await channel.RequestAsync(static _ => null,
                Method.PRODUCER_GET_STATS,
                null,
                @internal.ProducerId);
            var stats = response.NotNull().BodyAsProducer_GetStatsResponse().UnPack().Stats;

            return stats;
        }
    }

    /// <summary>
    /// Pause the Producer.
    /// </summary>
    public async Task PauseAsync()
    {
        logger.LogDebug("PauseAsync() | Producer:{ProducerId}", Id);

        await using (await closeLock.ReadLockAsync())
        {
            if (Closed)
            {
                throw new InvalidStateException("Producer closed");
            }

            await pauseLock.WaitAsync();
            try
            {
                await channel.RequestAsync(static _ => null,
                    Method.PRODUCER_PAUSE, 
                    null,
                    @internal.ProducerId);
                
                var wasPaused = Paused;

                Paused = true;

                // Emit observer event.
                if (!wasPaused)
                {
                    Observer.SafeEmit(static x=>x.Pause);
                }
            }
            catch (Exception ex)
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
    /// Resume the Producer.
    /// </summary>
    public async Task ResumeAsync()
    {
        logger.LogDebug("ResumeAsync() | Producer:{ProducerId}", Id);

        await using (await closeLock.ReadLockAsync())
        {
            if (Closed)
            {
                throw new InvalidStateException("Producer closed");
            }

            await pauseLock.WaitAsync();
            try
            {
                await channel.RequestAsync(static _ => null,
                    Method.PRODUCER_RESUME,
                    null,
                    @internal.ProducerId);
                
                var wasPaused = Paused;

                Paused = false;

                // Emit observer event.
                if (wasPaused)
                {
                    Observer.SafeEmit(static x=>x.Resume);
                }
            }
            catch (Exception ex)
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
    /// Enable 'trace' event.
    /// </summary>
    public async Task EnableTraceEventAsync(List<Antelcat.MediasoupSharp.FBS.Producer.TraceEventType> types)
    {
        logger.LogDebug("EnableTraceEventAsync() | Producer:{ProducerId}", Id);

        await using (await closeLock.ReadLockAsync())
        {
            if (Closed)
            {
                throw new InvalidStateException("Producer closed");
            }

            // Fire and forget
            channel.RequestAsync(bufferBuilder => EnableTraceEventRequest.Pack(bufferBuilder,
                        new EnableTraceEventRequestT
                        {
                            Events = types ?? []
                        }).Value,
                    Method.CONSUMER_ENABLE_TRACE_EVENT,
                    Antelcat.MediasoupSharp.FBS.Request.Body.Consumer_EnableTraceEventRequest,
                    @internal.ProducerId)
                .ContinueWithOnFaultedHandleLog(logger);
        }
    }

    /// <summary>
    /// Send RTP packet (just valid for Producers created on a DirectTransport).
    /// </summary>
    public async Task SendAsync(byte[] rtpPacket)
    {
        await using (await closeLock.ReadLockAsync())
        {
            if (Closed)
            {
                throw new InvalidStateException("Producer closed");
            }

            // Fire and forget
            channel.NotifyAsync(bufferBuilder => 
                    SendNotification.CreateSendNotification(bufferBuilder,
                    SendNotification.CreateDataVectorBlock(bufferBuilder, rtpPacket)
                    ).Value, Event.PRODUCER_SEND,
                Antelcat.MediasoupSharp.FBS.Notification.Body.Producer_SendNotification,
                @internal.ProducerId,
                1024 + rtpPacket.Length
            ).ContinueWithOnFaultedHandleLog(logger);
        }
    }

    public async Task AddConsumerAsync(IConsumer consumer)
    {
        await using (await closeLock.ReadLockAsync())
        {
            if (Closed)
            {
                throw new InvalidStateException("Producer closed");
            }

            consumers[consumer.Id] = consumer;
        }
    }

    public async Task RemoveConsumerAsync(string consumerId)
    {
        logger.LogDebug("RemoveConsumer() | Producer:{ProducerId} ConsumerId:{ConsumerId}", Id, consumerId);

        await using (await closeLock.ReadLockAsync())
        {
            // 关闭后也允许移除
            consumers.Remove(consumerId);
        }
    }

    #region Event Handlers

    private void HandleWorkerNotifications()
    {
        channel.OnNotification += OnNotificationHandle;
    }

    private void OnNotificationHandle(string handlerId, Event @event, Notification notification)
    {
        if (handlerId != Id)
        {
            return;
        }

        switch (@event)
        {
            case Event.PRODUCER_SCORE:
            {
                var scoreNotification = notification.BodyAsProducer_ScoreNotification();
                var score             = scoreNotification.UnPack().Scores;
                Score = score;

                this.SafeEmit(static x => x.Score, score);

                // Emit observer event.
                Observer.SafeEmit(static x=> x.Score, score);

                break;
            }
            case Event.PRODUCER_VIDEO_ORIENTATION_CHANGE:
            {
                var videoOrientationChangeNotification =
                    notification.BodyAsProducer_VideoOrientationChangeNotification();
                var videoOrientation = videoOrientationChangeNotification.UnPack();

                this.SafeEmit(static x=>x.VideoOrientationChange, videoOrientation);

                // Emit observer event.
                Observer.SafeEmit(static x=>x.VideoOrientationChange, videoOrientation);

                break;
            }
            case Event.PRODUCER_TRACE:
            {
                var traceNotification = notification.BodyAsProducer_TraceNotification();
                var trace             = traceNotification.UnPack();

                this.SafeEmit(static x => x.Trace, trace);

                // Emit observer event.
                Observer.SafeEmit(static x => x.Trace, trace);

                break;
            }
            default:
            {
                logger.LogError("OnNotificationHandle() | Ignoring unknown event: {Event}", @event);
                break;
            }
        }
    }

    private void HandleListenerError() =>
        this.On(static x => x.ListenerError, tuple =>
        {
            logger.LogError(tuple.error,
                "event listener threw an error [eventName:{EventName}]:",
                tuple.eventName);
        });

    #endregion Event Handlers

    #region Private Methods

#pragma warning disable VSTHRD100 // Avoid async void methods
    private async void CheckConsumers(object? state)
#pragma warning restore VSTHRD100 // Avoid async void methods
    {
        logger.LogDebug("CheckConsumer() | Producer:{ProducerId} ConsumerCount:{Count}", @internal.ProducerId,
            consumers.Count);

        // NOTE: 使用写锁
        await using (await closeLock.WriteLockAsync())
        {
            if (Closed)
            {
                checkConsumersTimer?.Dispose();
                return;
            }

            if (consumers.Count == 0)
            {
                CloseInternal();
                checkConsumersTimer?.Dispose();
            }
            else
            {
                checkConsumersTimer?.Change(
                    TimeSpan.FromSeconds(CheckConsumersTimeSeconds),
                    TimeSpan.FromMilliseconds(-1)
                );
            }
        }
    }

    #endregion Private Methods
}