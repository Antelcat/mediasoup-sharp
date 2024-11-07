﻿using System.Text;
using Antelcat.AutoGen.ComponentModel.Diagnostic;
using Antelcat.MediasoupSharp.Internals.Extensions;
using FBS.DataProducer;
using FBS.Notification;
using FBS.Request;
using FBS.SctpParameters;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Threading;

namespace Antelcat.MediasoupSharp;

using DataProducerObserver = EnhancedEventEmitter<DataProducerObserverEvents>;

public class DataProducerOptions<TDataProducerAppData>
{
    /// <summary>
    /// DataProducer id (just for Router.pipeToRouter() method).
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// SCTP parameters defining how the endpoint is sending the data.
    /// </summary>
    public SctpStreamParameters? SctpStreamParameters { get; set; }

    /// <summary>
    /// A label which can be used to distinguish this DataChannel from others.
    /// </summary>
    public string? Label { get; set; }

    /// <summary>
    /// Name of the sub-protocol used by this DataChannel.
    /// </summary>
    public string? Protocol { get; set; }

    /// <summary>
    /// Whether the data producer must start in paused mode. Default false.
    /// </summary>
    public bool Paused { get; set; }

    /// <summary>
    /// Custom application data.
    /// </summary>
    public TDataProducerAppData? AppData { get; set; }
}

public abstract class DataProducerEvents
{
    public object? TransportClose;

    public (string, Exception) ListenerError;

    // Private events.
    internal object? close;
}

public abstract class DataProducerObserverEvents
{
    public object? Close;
    public object? Pause;
    public object? Resume;
}

public class DataProducerInternal : TransportInternal
{
    /// <summary>
    /// DataProducer id.
    /// </summary>
    public required string DataProducerId { get; init; }
}

public class DataProducerData
{
    public FBS.DataProducer.Type Type { get; set; }

    /// <summary>
    /// SCTP stream parameters.
    /// </summary>
    public SctpStreamParametersT? SctpStreamParameters { get; init; }

    /// <summary>
    /// DataChannel label.
    /// </summary>
    public required string Label { get; init; }

    /// <summary>
    /// DataChannel protocol.
    /// </summary>
    public required string Protocol { get; init; }
}

[AutoExtractInterface]
public class DataProducer<TDataProducerAppData> : EnhancedEventEmitter<DataProducerEvents>, IDataProducer
    where TDataProducerAppData : new()
{
    /// <summary>
    /// Logger.
    /// </summary>
    private readonly ILogger logger = new Logger<DataProducer<TDataProducerAppData>>();

    /// <summary>
    /// Close flag.
    /// </summary>
    private bool closed;

    private readonly AsyncReaderWriterLock closeLock = new();

    /// <summary>
    /// Paused flag.
    /// </summary>
    private bool paused;

    /// <summary>
    /// Internal data.
    /// </summary>
    private readonly DataProducerInternal @internal;

    /// <summary>
    /// DataProducer id.
    /// </summary>
    public string Id => @internal.DataProducerId;

    /// <summary>
    /// DataProducer data.
    /// </summary>
    public DataProducerData Data { get; }

    /// <summary>
    /// Channel instance.
    /// </summary>
    private readonly IChannel channel;

    /// <summary>
    /// App custom data.
    /// </summary>
    public TDataProducerAppData AppData { get; }

    /// <summary>
    /// Observer instance.
    /// </summary>
    public DataProducerObserver Observer { get; } = new();

    /// <summary>
    /// <para>Events:</para>
    /// <para>@emits <see cref="DataProducerEvents.TransportClose"/></para>
    /// <para>@emits <see cref="DataProducerEvents.close"/></para>
    /// <para>Observer events:</para>
    /// <para>@emits <see cref="DataProducerObserverEvents.Close"/></para>
    /// </summary>
    public DataProducer(
        DataProducerInternal @internal,
        DataProducerData data,
        IChannel channel,
        bool paused,
        TDataProducerAppData? appData
    )
    {
        this.@internal = @internal;
        Data           = data;
        this.channel   = channel;
        this.paused    = paused;
        AppData        = appData ?? new();

        HandleWorkerNotifications();
    }

    /// <summary>
    /// Close the DataProducer.
    /// </summary>
    public async Task CloseAsync()
    {
        logger.LogDebug("CloseAsync() | DataProducer:{DataProducerId}", Id);

        await using (await closeLock.WriteLockAsync())
        {
            if (closed)
            {
                return;
            }

            closed = true;

            // Remove notification subscriptions.
            //_channel.OnNotification -= OnNotificationHandle;

            // Build Request
            var bufferBuilder = channel.BufferPool.Get();

            var closeDataProducerRequest = new FBS.Transport.CloseDataProducerRequestT
            {
                DataProducerId = @internal.DataProducerId
            };

            var closeDataProducerRequestOffset =
                FBS.Transport.CloseDataProducerRequest.Pack(bufferBuilder, closeDataProducerRequest);

            // Fire and forget
            channel.RequestAsync(bufferBuilder, Method.TRANSPORT_CLOSE_DATAPRODUCER,
                FBS.Request.Body.Transport_CloseDataProducerRequest,
                closeDataProducerRequestOffset.Value,
                @internal.TransportId
            ).ContinueWithOnFaultedHandleLog(logger);

            this.Emit(static x => x.close);

            // Emit observer event.
            Observer.Emit(static x=>x.Close);
        }
    }

    /// <summary>
    /// Transport was closed.
    /// </summary>
    public async Task TransportClosedAsync()
    {
        logger.LogDebug("TransportClosedAsync() | DataProducer:{DataProducerId}", Id);

        await using (await closeLock.WriteLockAsync())
        {
            if (closed)
            {
                return;
            }

            closed = true;

            // Remove notification subscriptions.
            //_channel.OnNotification -= OnNotificationHandle;

            this.Emit(static x=>x.TransportClose);

            // Emit observer event.
            Observer.Emit(static x=>x.Close);
        }
    }

    /// <summary>
    /// Dump DataProducer.
    /// </summary>
    public async Task<FBS.DataProducer.DumpResponseT> DumpAsync()
    {
        logger.LogDebug($"{nameof(DumpAsync)}() | DataProducer:{{DataProducerId}}", Id);

        await using (await closeLock.ReadLockAsync())
        {
            if (closed)
            {
                throw new InvalidStateException("DataProducer closed");
            }

            // Build Request
            var bufferBuilder = channel.BufferPool.Get();

            var response = await channel.RequestAsync(bufferBuilder, Method.DATAPRODUCER_DUMP,
                null,
                null,
                @internal.DataProducerId);

            /* Decode Response. */
            var data = response.NotNull().BodyAsDataProducer_DumpResponse().UnPack();
            return data;
        }
    }

    /// <summary>
    /// Get DataProducer stats. Return: DataProducerStat[]
    /// </summary>
    public async Task<FBS.DataProducer.GetStatsResponseT[]> GetStatsAsync()
    {
        logger.LogDebug($"{nameof(GetStatsAsync)}() | DataProducer:{{DataProducerId}}", Id);

        await using (await closeLock.ReadLockAsync())
        {
            if (closed)
            {
                throw new InvalidStateException("DataProducer closed");
            }

            // Build Request
            var bufferBuilder = channel.BufferPool.Get();

            var response = await channel.RequestAsync(bufferBuilder, Method.DATAPRODUCER_GET_STATS,
                null,
                null,
                @internal.DataProducerId);

            /* Decode Response. */
            var data = response.NotNull().BodyAsDataProducer_GetStatsResponse().UnPack();
            return [data];
        }
    }

    /// <summary>
    /// Pause the DataProducer.
    /// </summary>
    public async Task PauseAsync()
    {
        logger.LogDebug($"{nameof(PauseAsync)}() | DataProducer:{{DataProducerId}}", Id);

        await using (await closeLock.ReadLockAsync())
        {
            if (closed)
            {
                throw new InvalidStateException("DataProducer closed");
            }

            // Build Request
            var bufferBuilder = channel.BufferPool.Get();

            await channel.RequestAsync(bufferBuilder, Method.DATACONSUMER_PAUSE,
                null,
                null,
                @internal.DataProducerId);

            var wasPaused = paused;

            paused = true;

            // Emit observer event.
            if (!wasPaused)
            {
                Observer.Emit(static x=>x.Pause);
            }
        }
    }

    /// <summary>
    /// Resume the DataProducer.
    /// </summary>
    public async Task ResumeAsync()
    {
        logger.LogDebug($"{nameof(ResumeAsync)}() | DataProducer:{{DataProducerId}}", Id);

        await using (await closeLock.ReadLockAsync())
        {
            if (closed)
            {
                throw new InvalidStateException("DataConsumer closed");
            }

            // Build Request
            var bufferBuilder = channel.BufferPool.Get();

            await channel.RequestAsync(bufferBuilder, Method.DATACONSUMER_RESUME,
                null,
                null,
                @internal.DataProducerId);

            var wasPaused = paused;

            paused = false;

            // Emit observer event.
            if (wasPaused)
            {
                Observer.Emit(static x=>x.Resume);
            }
        }
    }

    /// <summary>
    /// Send data (just valid for DataProducers created on a DirectTransport).
    /// </summary>
    public async Task SendAsync(string message,
                                uint? ppid = null,
                                List<ushort>? subchannels = null,
                                ushort? requiredSubchannel = null)
    {
        logger.LogDebug($"{nameof(SendAsync)}() | DataProducer:{{DataProducerId}}", Id);

        /*
         * +-------------------------------+----------+
         * | Value                         | SCTP     |
         * |                               | PPID     |
         * +-------------------------------+----------+
         * | WebRTC String                 | 51       |
         * | WebRTC Binary Partial         | 52       |
         * | (Deprecated)                  |          |
         * | WebRTC Binary                 | 53       |
         * | WebRTC String Partial         | 54       |
         * | (Deprecated)                  |          |
         * | WebRTC String Empty           | 56       |
         * | WebRTC Binary Empty           | 57       |
         * +-------------------------------+----------+
         */

        ppid ??= !message.IsNullOrEmpty() ? 51u : 56u;

        // Ensure we honor PPIDs.
        if (ppid == 56)
        {
            message = " ";
        }

        await SendInternalAsync(Encoding.UTF8.GetBytes(message), ppid.Value, subchannels, requiredSubchannel);
    }

    /// <summary>
    /// Send data (just valid for DataProducers created on a DirectTransport).
    /// </summary>
    public async Task SendAsync(byte[] message, uint? ppid, List<ushort>? subchannels, ushort? requiredSubchannel)
    {
        logger.LogDebug("SendAsync() | DataProducer:{DataProducerId}", Id);

        ppid ??= !message.IsNullOrEmpty() ? 53u : 57u;

        // Ensure we honor PPIDs.
        if (ppid == 57)
        {
            message = new byte[1];
        }

        await SendInternalAsync(message, ppid.Value, subchannels, requiredSubchannel);
    }

    private async Task SendInternalAsync(byte[] data, uint ppid, List<ushort>? subchannels, ushort? requiredSubchannel)
    {
        await using (await closeLock.ReadLockAsync())
        {
            if (closed)
            {
                throw new InvalidStateException("DataProducer closed");
            }

            // Build Request
            var bufferBuilder = channel.BufferPool.Get();

            var sendNotification = new SendNotificationT
            {
                Ppid               = ppid,
                Data               = data.ToList(),
                Subchannels        = subchannels ?? [],
                RequiredSubchannel = requiredSubchannel
            };

            var sendNotificationOffset = SendNotification.Pack(bufferBuilder, sendNotification);

            // Fire and forget
            channel.NotifyAsync(bufferBuilder, Event.PRODUCER_SEND,
                FBS.Notification.Body.DataProducer_SendNotification,
                sendNotificationOffset.Value,
                @internal.DataProducerId
            ).ContinueWithOnFaultedHandleLog(logger);
        }
    }

    #region Event Handlers

    private static void HandleWorkerNotifications()
    {
        // No need to subscribe to any event.
    }

    #endregion Event Handlers
}