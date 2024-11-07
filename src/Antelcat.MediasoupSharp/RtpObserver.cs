using Antelcat.MediasoupSharp.Internals.Extensions;
using FBS.Notification;
using FBS.Request;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Threading;

namespace Antelcat.MediasoupSharp;

using RtpObserverObserver = IEnhancedEventEmitter<RtpObserverObserverEvents>;

public abstract class RtpObserverEvents
{
    public object? RouterClose;

    public required (string, Exception) ListenerError;

    // Private events.
    internal object? close;
}

public abstract class RtpObserverObserverEvents
{
    public          object?   Close;
    public          object?   Pause;
    public          object?   Resume;
    public required IProducer AddProducer;
    public required IProducer RemoveProducer;
}

public class RtpObserverConstructorOptions<TRtpObserverAppData>
{
    public required RtpObserverObserverInternal    Internal        { get; set; }
    public required IChannel                       Channel         { get; set; }
    public          TRtpObserverAppData?           AppData         { get; set; }
    public required Func<string, Task<IProducer?>> GetProducerById { get; set; }
}

public class RtpObserverObserverInternal : RouterInternal
{
    public required string RtpObserverId { get; set; }
}

public class RtpObserverAddRemoveProducerOptions
{
    /// <summary>
    /// The id of the Producer to be added or removed.
    /// </summary>
    public required string ProducerId { get; set; }
}

public abstract class RtpObserver<TRtpObserverAppData, TEvents, TObserver>
    : EnhancedEventEmitter<TEvents>, IRtpObserver
    where TRtpObserverAppData : new()
    where TEvents : RtpObserverEvents
    where TObserver : RtpObserverObserver
{
    /// <summary>
    /// Logger.
    /// </summary>
    private readonly ILogger logger = new Logger<RtpObserver<TRtpObserverAppData, TEvents, TObserver>>();

    /// <summary>
    /// Whether the Producer is closed.
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
    public RtpObserverObserverInternal Internal { get; }

    /// <summary>
    /// Channel instance.
    /// </summary>
    protected readonly IChannel Channel;

    /// <summary>
    /// App custom data.
    /// </summary>
    public TRtpObserverAppData AppData { get; }

    /// <summary>
    /// Method to retrieve a Producer.
    /// </summary>
    protected readonly Func<string, Task<IProducer?>> GetProducerById;

    /// <summary>
    /// Observer instance.
    /// </summary>
    public TObserver Observer { get; }

    /// <summary>
    /// <para>Events:</para>
    /// <para>@emits <see cref="RtpObserverEvents.RouterClose"/></para>
    /// <para>@emits <see cref="RtpObserverEvents.close"/></para>
    /// <para>Observer events:</para>
    /// <para>@emits <see cref="RtpObserverObserverEvents.Close"/></para>
    /// <para>@emits <see cref="RtpObserverObserverEvents.Pause"/></para>
    /// <para>@emits <see cref="RtpObserverObserverEvents.Resume"/></para>
    /// <para>@emits <see cref="RtpObserverObserverEvents.AddProducer"/> - (producer: Producer)</para>
    /// <para>@emits <see cref="RtpObserverObserverEvents.RemoveProducer"/> - (producer: Producer)</para>
    /// </summary>
    protected RtpObserver(
        RtpObserverConstructorOptions<TRtpObserverAppData> options,
        TObserver observer
    )
    {
        Internal        = options.Internal;
        Channel         = options.Channel;
        AppData         = options.AppData ?? new();
        GetProducerById = options.GetProducerById;
        Observer        = observer;
        pauseLock.Set();

        HandleWorkerNotifications();
    }

    /// <summary>
    /// Close the RtpObserver.
    /// </summary>
    public async Task CloseAsync()
    {
        logger.LogDebug($"{nameof(CloseAsync)}() | RtpObserverId:{{RtpObserverId}}", Internal.RtpObserverId);

        await using (await closeLock.WriteLockAsync())
        {
            if (closed) return;

            closed = true;

            // Remove notification subscriptions.
            Channel.OnNotification -= OnNotificationHandle;

            // Build Request
            var bufferBuilder = Channel.BufferPool.Get();

            var closeRtpObserverRequest = new FBS.Router.CloseRtpObserverRequestT
            {
                RtpObserverId = Internal.RtpObserverId
            };

            var closeRtpObserverRequestOffset =
                FBS.Router.CloseRtpObserverRequest.Pack(bufferBuilder, closeRtpObserverRequest);

            // Fire and forget
            Channel.RequestAsync(bufferBuilder, Method.ROUTER_CLOSE_RTPOBSERVER,
                FBS.Request.Body.Router_CloseRtpObserverRequest,
                closeRtpObserverRequestOffset.Value,
                Internal.RouterId
            ).ContinueWithOnFaultedHandleLog(logger);

            this.Emit(static x => x.close);

            // Emit observer event.
            Observer.Emit(static x=>x.Close);
        }
    }

    /// <summary>
    /// Router was closed.
    /// </summary>
    public async Task RouterClosedAsync()
    {
        logger.LogDebug("RouterClosed() | RtpObserverId:{RtpObserverId}", Internal.RtpObserverId);

        await using (await closeLock.WriteLockAsync())
        {
            if (closed)
            {
                return;
            }

            closed = true;

            // Remove notification subscriptions.
            Channel.OnNotification -= OnNotificationHandle;

            this.Emit(static x => x.RouterClose);

            // Emit observer event.
            Observer.Emit(static x=>x.Close);
        }
    }

    /// <summary>
    /// Pause the RtpObserver.
    /// </summary>
    public async Task PauseAsync()
    {
        logger.LogDebug("PauseAsync() | RtpObserverId:{RtpObserverId}", Internal.RtpObserverId);

        await using (await closeLock.ReadLockAsync())
        {
            if (closed)
            {
                throw new InvalidStateException("PauseAsync()");
            }

            await pauseLock.WaitAsync();
            try
            {
                var wasPaused = paused;

                // Build Request
                var bufferBuilder = Channel.BufferPool.Get();

                // Fire and forget
                Channel.RequestAsync(bufferBuilder, Method.RTPOBSERVER_PAUSE,
                    null,
                    null,
                    Internal.RtpObserverId
                ).ContinueWithOnFaultedHandleLog(logger);

                paused = true;

                // Emit observer event.
                if (!wasPaused)
                {
                    Observer.Emit(static x=>x.Pause);
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
    /// Resume the RtpObserver.
    /// </summary>
    public async Task ResumeAsync()
    {
        logger.LogDebug("ResumeAsync() | RtpObserverId:{RtpObserverId}", Internal.RtpObserverId);

        await using (await closeLock.ReadLockAsync())
        {
            if (closed)
            {
                throw new InvalidStateException("ResumeAsync()");
            }

            await pauseLock.WaitAsync();
            try
            {
                var wasPaused = paused;

                // Build Request
                var bufferBuilder = Channel.BufferPool.Get();

                // Fire and forget
                Channel.RequestAsync(bufferBuilder, Method.RTPOBSERVER_RESUME,
                    null,
                    null,
                    Internal.RtpObserverId
                ).ContinueWithOnFaultedHandleLog(logger);

                paused = false;

                // Emit observer event.
                if (wasPaused)
                {
                    Observer.Emit(static x=>x.Resume);
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
    /// Add a Producer to the RtpObserver.
    /// </summary>
    public async Task AddProducerAsync(RtpObserverAddRemoveProducerOptions rtpObserverAddRemoveProducerOptions)
    {
        logger.LogDebug("AddProducerAsync() | RtpObserverId:{RtpObserverId} ProducerId:{ProducerId}",
            Internal.RtpObserverId, rtpObserverAddRemoveProducerOptions.ProducerId);

        await using (await closeLock.ReadLockAsync())
        {
            if (closed)
            {
                throw new InvalidStateException("RepObserver closed");
            }

            var producer = await GetProducerById(rtpObserverAddRemoveProducerOptions.ProducerId);
            if (producer == null)
            {
                return;
            }

            // Build Request
            var bufferBuilder = Channel.BufferPool.Get();

            var addProducerRequest = new FBS.RtpObserver.AddProducerRequestT
            {
                ProducerId = rtpObserverAddRemoveProducerOptions.ProducerId
            };

            var addProducerRequestOffset = FBS.RtpObserver.AddProducerRequest.Pack(bufferBuilder, addProducerRequest);

            // Fire and forget
            Channel.RequestAsync(bufferBuilder, Method.RTPOBSERVER_ADD_PRODUCER,
                FBS.Request.Body.RtpObserver_AddProducerRequest,
                addProducerRequestOffset.Value,
                Internal.RtpObserverId
            ).ContinueWithOnFaultedHandleLog(logger);

            // Emit observer event.
            Observer.Emit(static x => x.AddProducer, producer);
        }
    }

    /// <summary>
    /// Remove a Producer from the RtpObserver.
    /// </summary>
    public async Task RemoveProducerAsync(RtpObserverAddRemoveProducerOptions rtpObserverAddRemoveProducerOptions)
    {
        logger.LogDebug($"{nameof(RemoveProducerAsync)}() | RtpObserverId:{{RtpObserverId}}", Internal.RtpObserverId);

        await using (await closeLock.ReadLockAsync())
        {
            if (closed)
            {
                throw new InvalidStateException("RepObserver closed");
            }

            var producer = await GetProducerById(rtpObserverAddRemoveProducerOptions.ProducerId);
            if (producer == null)
            {
                return;
            }

            // Build Request
            var bufferBuilder = Channel.BufferPool.Get();

            var removeProducerRequest = new FBS.RtpObserver.RemoveProducerRequestT
            {
                ProducerId = rtpObserverAddRemoveProducerOptions.ProducerId
            };

            var removeProducerRequestOffset =
                FBS.RtpObserver.RemoveProducerRequest.Pack(bufferBuilder, removeProducerRequest);

            // Fire and forget
            Channel.RequestAsync(bufferBuilder, Method.RTPOBSERVER_REMOVE_PRODUCER,
                FBS.Request.Body.RtpObserver_RemoveProducerRequest,
                removeProducerRequestOffset.Value,
                Internal.RtpObserverId
            ).ContinueWithOnFaultedHandleLog(logger);

            // Emit observer event.
            Observer.Emit(static x => x.RemoveProducer, producer);
        }
    }

    #region Event Handlers

    private void HandleWorkerNotifications()
    {
        Channel.OnNotification += OnNotificationHandle;
    }

    protected virtual void OnNotificationHandle(string handlerId, Event @event, Notification notification)
    {
    }

    #endregion Event Handlers
}

public interface IRtpObserver : IEnhancedEventEmitter<RtpObserverEvents>
{
    RtpObserverObserverInternal Internal { get; }

    Task RouterClosedAsync();
}