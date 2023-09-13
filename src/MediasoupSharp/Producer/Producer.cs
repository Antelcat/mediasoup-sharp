using MediasoupSharp.RtpParameters;
using Microsoft.Extensions.Logging;

namespace MediasoupSharp.Producer;

internal class Producer<TProducerAppData> : Producer
{
    public Producer(
        ProducerInternal @internal,
        ProducerData data,
        Channel.Channel channel,
        PayloadChannel.PayloadChannel payloadChannel,
        TProducerAppData? appData,
        bool paused)
        : base(
            @internal,
            data,
            channel,
            payloadChannel,
            appData,
            paused)
    {
    }

    public new TProducerAppData AppData
    {
        get => (TProducerAppData)base.AppData;
        set => base.AppData = value!;
    }
}

internal class Producer : EnhancedEventEmitter<ProducerEvents>
{
    private readonly ILogger? logger;
    /// <summary>
    /// Internal data.
    /// </summary>
    private readonly ProducerInternal @internal;

    /// <summary>
    /// Producer data.
    /// </summary>
    private readonly ProducerData data;

    /// <summary>
    /// Channel instance.
    /// </summary>
    private readonly Channel.Channel channel;

    /// <summary>
    /// Channel instance.
    /// </summary>
    private readonly PayloadChannel.PayloadChannel payloadChannel;

    /// <summary>
    /// Whether the Producer is closed.
    /// </summary>
    public bool Closed { get; private set; }

    /// <summary>
    /// App custom data.
    /// </summary>
    public object AppData { get; set; }

    /// <summary>
    /// Paused flag.
    /// </summary>
    public bool Paused { get; private set; }

    /// <summary>
    /// Current score.
    /// </summary>
    public List<ProducerScore> Score { get; private set; } = new();

    /// <summary>
    /// Observer instance.
    /// </summary>
    public EnhancedEventEmitter<ProducerObserverEvents> Observer { get; }
    
    internal Producer(
        ProducerInternal @internal,
        ProducerData data,
        Channel.Channel channel,
        PayloadChannel.PayloadChannel payloadChannel,
        object? appData,
        bool paused,
        ILoggerFactory? loggerFactory = null
    ) : base(loggerFactory)
    {
        logger              = loggerFactory?.CreateLogger(GetType());
        this.@internal      = @internal;
        this.data           = data;
        this.channel        = channel;
        this.payloadChannel = payloadChannel;
        AppData             = appData ?? new object();
        Paused              = paused;
        Observer            = new(loggerFactory);
        HandleWorkerNotifications();
    }

    /// <summary>
    /// Producer id.
    /// </summary>
    public string Id => @internal.ProducerId;

    public MediaKind Kind => data.Kind;

    public RtpParameters.RtpParameters RtpParameters => data.RtpParameters;

    public ProducerType Type => data.Type;

    internal RtpParameters.RtpParameters ConsumableRtpParameters => data.ConsumableRtpParameters;

    internal Channel.Channel ChannelForTesting => channel;


    /// <summary>
    /// Close the Producer.
    /// </summary>
    public void Close()
    {
        if (Closed)
        {
            return;
        }

        logger?.LogDebug("CloseAsync() | Producer:{Id}", Id);

        Closed = true;

        // Remove notification subscriptions.
        channel.RemoveAllListeners(@internal.ProducerId);
        payloadChannel.RemoveAllListeners(@internal.ProducerId);

        // TODO : Naming
        var reqData = new { producerId = @internal.ProducerId };

        // Fire and forget
        channel.Request("transport.closeProducer", @internal.TransportId, reqData)
            .ContinueWith(_ => { }, TaskContinuationOptions.OnlyOnFaulted);

        _ = Emit("@close");

        // Emit observer event.
        _ = Observer.SafeEmit("close");
    }

    /// <summary>
    /// Transport was closed.
    /// </summary>
    internal void TransportClosed()
    {
        if (Closed)
        {
            return;
        }

        logger?.LogDebug("CloseAsync() | Producer:{Id}", Id);

        Closed = true;

        // Remove notification subscriptions.
        channel.RemoveAllListeners(@internal.ProducerId);
        payloadChannel.RemoveAllListeners(@internal.ProducerId);

        _ = SafeEmit("transportclose");

        // Emit observer event.
        _ = Observer.SafeEmit("close");
    }

    /// <summary>
    /// Dump Producer.
    /// </summary>
    public async Task<object> DumpAsync()
    {
        logger?.LogDebug("DumpAsync() | Producer:{Id}", Id);

        return (await channel.Request("producer.dump", @internal.ProducerId))!;
    }

    /// <summary>
    /// Get DataProducer stats.
    /// </summary>
    public async Task<List<ProducerStat>> GetStatsAsync()
    {
        logger?.LogDebug("GetStatsAsync() | Producer:{Id}", Id);

        return (await channel.Request("producer.getStats", @internal.ProducerId) as List<ProducerStat>)!;
    }

    /// <summary>
    /// Pause the Producer.
    /// </summary>
    public async Task PauseAsync()
    {
        logger?.LogDebug("PauseAsync() | Producer:{Id}", Id);

        var wasPaused = Paused;

        await channel.Request("producer.pause", @internal.ProducerId);

        Paused = true;

        // Emit observer event.
        if (!wasPaused)
        {
            await Observer.SafeEmit("pause");
        }
    }

    /// <summary>
    /// Resume the Producer.
    /// </summary>
    public async Task ResumeAsync()
    {
        logger?.LogDebug("ResumeAsync() | Producer:{Id}", Id);

        var wasPaused = Paused;

        await channel.Request("producer.resume", @internal.ProducerId);

        Paused = false;

        // Emit observer event.
        if (wasPaused)
        {
            await Observer.Emit("resume");
        }
    }

    /// <summary>
    /// Enable 'trace' event.
    /// </summary>
    public async Task EnableTraceEventAsync(ProducerTraceEventType[] types)
    {
        logger?.LogDebug("EnableTraceEventAsync() | Producer:{Id}", Id);

        // TODO : Naming
        var reqData = new { types };

        await channel.Request("producer.enableTraceEvent", @internal.ProducerId, reqData);
    }

    /// <summary>
    /// Send RTP packet (just valid for Producers created on a DirectTransport).
    /// </summary>
    /// <param name="rtpPacket"></param>
    public void Send(byte[] rtpPacket)
    {
        payloadChannel.Notify("producer.send", @internal.ProducerId, null, rtpPacket);
    }


    private void HandleWorkerNotifications()
    {
        channel.On(@internal.ProducerId, async args =>
        {
            var @event = args![0] as string;
            var data = args[1];
            switch (@event)
            {
                case "score":
                {
                    var score = (data as List<ProducerScore>)!;

                    Score = score;

                    await SafeEmit("score", score);

                    // Emit observer event.
                    await Observer.SafeEmit("score", score);

                    break;
                }

                case "videoorientationchange":
                {
                    var videoOrientation = (data as ProducerVideoOrientation)!;

                    await SafeEmit("videoorientationchange", videoOrientation);

                    // Emit observer event.
                    await Observer.SafeEmit("videoorientationchange", videoOrientation);

                    break;
                }

                case "trace":
                {
                    var trace = (data as ProducerTraceEventData)!;

                    await SafeEmit("trace", trace);

                    // Emit observer event.
                    await Observer.SafeEmit("trace", trace);

                    break;
                }

                default:
                {
                    logger?.LogError("ignoring unknown event {E}", @event);
                    break;
                }
            }
        });
       
    }
}