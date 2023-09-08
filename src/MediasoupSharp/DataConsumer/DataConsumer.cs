using System.Security.Cryptography.X509Certificates;
using MediasoupSharp.Exceptions;
using MediasoupSharp.SctpParameters;
using Microsoft.Extensions.Logging;

namespace MediasoupSharp.DataConsumer;

internal class DataConsumer<TDataConsumerAppData> : DataConsumer
{
    public DataConsumer(
        ILoggerFactory loggerFactory,
        DataConsumerInternal @internal,
        DataConsumerData data,
        Channel.Channel channel,
        PayloadChannel.PayloadChannel payloadChannel,
        TDataConsumerAppData? appData)
        : base(loggerFactory,
            @internal,
            data,
            channel,
            payloadChannel,
            appData)
    { }

    public new TDataConsumerAppData AppData
    {
        get => (TDataConsumerAppData)base.AppData;
        set => base.AppData = value;
    }
}

internal class DataConsumer : EnhancedEventEmitter<DataConsumerEvents>
{
    /// <summary>
    /// Logger.
    /// </summary>
    private readonly ILogger logger;


    /// <summary>
    /// Internal data.
    /// </summary>
    private readonly DataConsumerInternal @internal;

    /// <summary>
    /// DataConsumer id.
    /// </summary>
    public string DataConsumerId => @internal.DataConsumerId;

    /// <summary>
    /// DataChannel data.
    /// </summary>
    private readonly DataConsumerData data;

    /// <summary>
    /// Channel instance.
    /// </summary>
    private readonly Channel.Channel channel;

    /// <summary>
    /// PayloadChannel instance.
    /// </summary>
    private readonly PayloadChannel.PayloadChannel payloadChannel;

    /// <summary>
    /// Whether the DataConsumer is closed.
    /// </summary>
    public bool Closed { get; private set; }

    /// <summary>
    /// App custom data.
    /// </summary>
    public object AppData { get; set; }

    /// <summary>
    /// Observer instance.
    /// </summary>
    internal EnhancedEventEmitter<DataConsumerObserverEvents> Observer => observer ??= new();

    #region Extra

    private EnhancedEventEmitter<DataConsumerObserverEvents>? observer;
    public override ILoggerFactory LoggerFactory
    {
        init
        {
            observer = new EnhancedEventEmitter<DataConsumerObserverEvents>
            {
                LoggerFactory = value
            };
            base.LoggerFactory = value;
        }
    }

    #endregion
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="loggerFactory"></param>
    /// <param name="internal"></param>
    /// <param name="data"></param>
    /// <param name="channel"></param>
    /// <param name="payloadChannel"></param>
    /// <param name="appData"></param>
    public DataConsumer(ILoggerFactory loggerFactory,
        DataConsumerInternal @internal,
        DataConsumerData data,
        Channel.Channel channel,
        PayloadChannel.PayloadChannel payloadChannel,
        object? appData
    ) 
    {
        logger = loggerFactory.CreateLogger(GetType());

        this.@internal = @internal;
        this.data = data;
        this.channel = channel;
        this.payloadChannel = payloadChannel;
        AppData = appData ?? new();
  
        HandleWorkerNotifications();
    }

    public string Id => @internal.DataConsumerId;

    public string DataProducerId => data.DataProducerId;

    public DataConsumerType Type => data.Type;

    public SctpStreamParameters? SctpStreamParameters => data.SctpStreamParameters;

    public string Label => data.Label;

    public string Protocol => data.Protocol;

    /// <summary>
    /// Close the DataConsumer.
    /// </summary>
    public void Close()
    {
        if (Closed)
        {
            return;
        }

        logger.LogDebug("CloseAsync() | DataConsumer:{DataConsumerId}", DataConsumerId);

        Closed = true;

        // Remove notification subscriptions.
        channel.RemoveAllListeners(@internal.DataConsumerId);
        payloadChannel.RemoveAllListeners(@internal.DataConsumerId);

        // TODO : Naming
        var reqData = new { dataConsumerId = @internal.DataConsumerId };

        // Fire and forget
        channel.Request("transport.closeDataConsumer", @internal.TransportId, reqData)
            .ContinueWith(_ => { }, TaskContinuationOptions.OnlyOnFaulted);

        _ = Emit("@close");

        // Emit observer event.
        _ = Observer.SafeEmit("close");
    }

    /// <summary>
    /// Transport was closed.
    /// </summary>
    public void TransportClosed()
    {
        if (Closed)
        {
            return;
        }

        logger.LogDebug("TransportClosedAsync() | DataConsumer:{DataConsumerId}", DataConsumerId);

        Closed = true;

        // Remove notification subscriptions.
        channel.RemoveAllListeners(@internal.DataConsumerId);
        payloadChannel.RemoveAllListeners(@internal.DataConsumerId);

        _ = SafeEmit("transportclose");

        // Emit observer event.
        _ = Observer.SafeEmit("close");
    }

    /// <summary>
    /// Dump DataConsumer.
    /// </summary>
    public async Task<object> DumpAsync()
    {
        logger.LogDebug("DumpAsync() | DataConsumer:{DataConsumerId}", DataConsumerId);

        return (await channel.Request("dataConsumer.dump", @internal.DataConsumerId))!;
    }

    /// <summary>
    /// Get DataConsumer stats. Return: DataConsumerStat[]
    /// </summary>
    public async Task<List<DataConsumerStat>> GetStatsAsync()
    {
        logger.LogDebug("GetStatsAsync() | DataConsumer:{DataConsumerId}", DataConsumerId);

        return (await channel.Request("dataConsumer.getStats", @internal.DataConsumerId) as List<DataConsumerStat>)!;
    }

    /// <summary>
    /// Set buffered amount low threshold.
    /// </summary>
    /// <param name="threshold"></param>
    /// <exception cref="InvalidStateException"></exception>
    public async Task SetBufferedAmountLowThresholdAsync(uint threshold)
    {
        logger.LogDebug("SetBufferedAmountLowThreshold() | Threshold:{Threshold}", threshold);

        // TODO : Naming
        var reqData = new { threshold };

        await channel.Request("dataConsumer.setBufferedAmountLowThreshold", @internal.DataConsumerId, reqData);
    }

    public async Task SendAsync(object message, int? ppid)
    {
        if (message is not (string or byte[]))
        {
            throw new TypeError("message must be a string or a Buffer");
        }

        logger.LogDebug("SendAsync() | DataConsumer:{DataConsumerId}", DataConsumerId);

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

        ppid ??= message is string str
            ? str.Length > 0 ? 51 : 56
            : ((byte[])message).Length > 0
                ? 53
                : 57;
        
        // Ensure we honor PPIDs.
        message = ppid switch
        {
            56 => " ",
            57 => new byte[1],
            _ => message
        };

        var requestData = ppid.Value.ToString();

        await payloadChannel.Request("dataConsumer.send", @internal.DataConsumerId, requestData, message);
    }
    
    /// <summary>
    /// Get buffered amount size.
    /// </summary>
    /// <returns></returns>
    public async Task<string> GetBufferedAmountAsync()
    {
        logger.LogDebug("GetBufferedAmountAsync()");

        var ret = (await channel.Request("dataConsumer.getBufferedAmount", @internal.DataConsumerId))! as dynamic;
        return ret.bufferedAmount;
    }


    private void HandleWorkerNotifications()
    {
        channel.On(@internal.DataConsumerId, async args =>
        {
            var @event = args![0] as string;
            var data = args[1] as dynamic;
            switch (@event)
            {
                case "dataproducerclose":
                {
                    if (Closed)
                    {
                        break;
                    }

                    Closed = true;

                    // Remove notification subscriptions.
                    channel.RemoveAllListeners(@internal.DataConsumerId);
                    payloadChannel.RemoveAllListeners(@internal.DataConsumerId);

                    _ = Emit("@dataproducerclose");
                    _ = SafeEmit("dataproducerclose");

                    // Emit observer event.
                    _ = Observer.SafeEmit("close");

                    break;
                }

                case "sctpsendbufferfull":
                {
                    _ = SafeEmit("sctpsendbufferfull");

                    break;
                }

                case "bufferedamountlow":
                {
                    _ = SafeEmit("bufferedamountlow", data.bufferedAmount);

                    break;
                }

                default:
                {
                    logger.LogError("ignoring unknown event {E} in channel listener", @event);
                    break;
                }
            }
        });

        payloadChannel.On(@internal.DataConsumerId, async args =>
        {
            var @event = args![0] as string;
            var data = args[1] as dynamic;
            var payload = args[2] as byte[];
            switch (@event)
            {
                case "message":
                {
                    if (Closed)
                    {
                        break;
                    }

                    var ppid = (int)data.ppid;
                    var message = payload!;

                    _ = SafeEmit("message", message, ppid);

                    break;
                }

                default:
                {
                    logger.LogError("ignoring unknown event {E} in payload channel listener", @event);
                    break;
                }
            }
        });
    }
}