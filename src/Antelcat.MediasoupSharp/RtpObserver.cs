using System.Runtime.InteropServices.ComTypes;
using Antelcat.AutoGen.ComponentModel.Diagnostic;
using Antelcat.MediasoupSharp.FBS.Notification;
using Antelcat.MediasoupSharp.FBS.Request;
using Antelcat.MediasoupSharp.Internals.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Threading;

namespace Antelcat.MediasoupSharp;

[AutoExtractInterface(
    NamingTemplate = nameof(IRtpObserver)
    )
]
public abstract class RtpObserverImpl<TRtpObserverAppData, TEvents, TObserver>
    : EnhancedEventEmitter<TEvents>, 
        IRtpObserver<
            TRtpObserverAppData, 
            TEvents, 
            TObserver
        >
    where TRtpObserverAppData : new()
    where TEvents : RtpObserverEvents
    where TObserver : RtpObserverObserver
{
    /// <summary>
    /// Logger.
    /// </summary>
    private readonly ILogger logger = new Logger<IRtpObserver>();

    /// <summary>
    /// Whether the Producer is closed.
    /// </summary>
    private bool closed;

    private readonly AsyncReaderWriterLock closeLock = new(null);

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

    public string Id     => Internal.RtpObserverId;
    public bool   Closed => closed;
    public bool   Paused => paused;

    /// <summary>
    /// App custom data.
    /// </summary>
    public TRtpObserverAppData AppData { get; set; }

    /// <summary>
    /// Method to retrieve a Producer.
    /// </summary>
    protected readonly Func<string, Task<IProducer?>> GetProducerById;

    /// <summary>
    /// Observer instance.
    /// </summary>
    public TObserver Observer { get; }

    /// <summary>
    /// <para>Events : <see cref="RtpObserverEvents"/></para>
    /// <para>Observer events : <see cref="RtpObserverObserverEvents"/></para>
    /// </summary>
    protected RtpObserverImpl(
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

            // Fire and forget
            Channel.RequestAsync(bufferBuilder => Antelcat.MediasoupSharp.FBS.Router.CloseRtpObserverRequest.Pack(
                    bufferBuilder, new Antelcat.MediasoupSharp.FBS.Router.CloseRtpObserverRequestT
                    {
                        RtpObserverId = Internal.RtpObserverId
                    }).Value,
                Method.ROUTER_CLOSE_RTPOBSERVER,
                Antelcat.MediasoupSharp.FBS.Request.Body.Router_CloseRtpObserverRequest,
                Internal.RouterId
            ).ContinueWithOnFaultedHandleLog(logger);

            this.Emit(static x => x.close);

            // Emit observer event.
            Observer.SafeEmit(static x => x.Close);
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

            this.SafeEmit(static x => x.RouterClose);

            // Emit observer event.
            Observer.SafeEmit(static x => x.Close);
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

                // Fire and forget
                Channel.RequestAsync(static _ => null, 
                    Method.RTPOBSERVER_PAUSE,
                    null,
                    Internal.RtpObserverId
                ).ContinueWithOnFaultedHandleLog(logger);

                paused = true;

                // Emit observer event.
                if (!wasPaused)
                {
                    Observer.SafeEmit(static x => x.Pause);
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

                // Fire and forget
                Channel.RequestAsync(static _ => null, Method.RTPOBSERVER_RESUME,
                    null,
                    Internal.RtpObserverId
                ).ContinueWithOnFaultedHandleLog(logger);

                paused = false;

                // Emit observer event.
                if (wasPaused)
                {
                    Observer.SafeEmit(static x => x.Resume);
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

            // Fire and forget
            Channel.RequestAsync(bufferBuilder => 
                    Antelcat.MediasoupSharp.FBS.RtpObserver.AddProducerRequest
                    .Pack(bufferBuilder, new Antelcat.MediasoupSharp.FBS.RtpObserver.AddProducerRequestT
                    {
                        ProducerId = rtpObserverAddRemoveProducerOptions.ProducerId
                    }).Value,
                Method.RTPOBSERVER_ADD_PRODUCER,
                Antelcat.MediasoupSharp.FBS.Request.Body.RtpObserver_AddProducerRequest,
                Internal.RtpObserverId
            ).ContinueWithOnFaultedHandleLog(logger);

            // Emit observer event.
            Observer.SafeEmit(static x => x.AddProducer, producer);
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

            // Fire and forget
            Channel.RequestAsync(bufferBuilder =>
                    Antelcat.MediasoupSharp.FBS.RtpObserver.RemoveProducerRequest.Pack(bufferBuilder,
                        new Antelcat.MediasoupSharp.FBS.RtpObserver.RemoveProducerRequestT
                        {
                            ProducerId = rtpObserverAddRemoveProducerOptions.ProducerId
                        }).Value, Method.RTPOBSERVER_REMOVE_PRODUCER,
                Antelcat.MediasoupSharp.FBS.Request.Body.RtpObserver_RemoveProducerRequest,
                Internal.RtpObserverId
            ).ContinueWithOnFaultedHandleLog(logger);

            // Emit observer event.
            Observer.SafeEmit(static x => x.RemoveProducer, producer);
        }
    }

    #region Event Handlers

    private void HandleWorkerNotifications()
    {
        Channel.OnNotification += OnNotificationHandle;
    }

    protected abstract void OnNotificationHandle(string handlerId, Event @event, Notification notification);

    #endregion Event Handlers
}