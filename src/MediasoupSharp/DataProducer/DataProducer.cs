using MediasoupSharp.SctpParameters;
using Microsoft.Extensions.Logging;

namespace MediasoupSharp.DataProducer;

public interface IDataProducer
{
    string Id { get; }

    object AppData { get; set; }

    internal EnhancedEventEmitter Observer { get; }

    DataProducerType Type { get; }

    SctpStreamParameters? SctpStreamParameters { get; }
    
    string Label { get; }
    
    string Protocol { get; }

    void Close();

    void TransportClosed();
}

internal class DataProducer<TDataProducerAppData> : EnhancedEventEmitter<DataProducerEvents> , IDataProducer
{
    private readonly ILogger? logger;
    
    /// <summary>
    /// Internal data.
    /// </summary>
    private readonly DataProducerInternal @internal;

    /// <summary>
    /// DataProducer data.
    /// </summary>
    private readonly DataProducerData data;

    /// <summary>
    /// Channel instance.
    /// </summary>
    private readonly Channel.Channel channel;

    /// <summary>
    /// PayloadChannel instance.
    /// </summary>
    private readonly PayloadChannel.PayloadChannel payloadChannel;

    /// <summary>
    /// Whether the DataProducer is closed.
    /// </summary>
    public bool Closed { get; private set; }

    /// <summary>
    /// App custom data.
    /// </summary>
    public object AppData
    {
        get => appData;
        set => appData = (TDataProducerAppData)value;
    }

    private TDataProducerAppData? appData;

    /// <summary>
    /// Observer instance.
    /// </summary>
    public EnhancedEventEmitter Observer { get; }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="internal"></param>
    /// <param name="data"></param>
    /// <param name="channel"></param>
    /// <param name="payloadChannel"></param>
    /// <param name="appData"></param>
    /// <param name="loggerFactory"></param>
    public DataProducer(
        DataProducerInternal @internal,
        DataProducerData data,
        Channel.Channel channel,
        PayloadChannel.PayloadChannel payloadChannel,
        TDataProducerAppData? appData,
        ILoggerFactory? loggerFactory = null
    )
    {
        logger              = loggerFactory?.CreateLogger(GetType());
        this.@internal      = @internal;
        this.data           = data;
        this.channel        = channel;
        this.payloadChannel = payloadChannel;
        AppData             = appData ?? typeof(TDataProducerAppData).New<TDataProducerAppData>()!;
        Observer            = new EnhancedEventEmitter<DataProducerObserverEvents>(loggerFactory);

        HandleWorkerNotifications();
    }

    public string Id => @internal.DataProducerId;

    public DataProducerType Type => data.Type;

    public SctpStreamParameters? SctpStreamParameters => data.SctpStreamParameters;

    public string Label => data.Label;

    public string Protocol => data.Protocol;


    /// <summary>
    /// Close the DataProducer.
    /// </summary>
    public void Close()
    {
        if (Closed)
        {
            return;
        }

        logger?.LogDebug("CloseAsync() | DataProducer:{Id}", Id);

        Closed = true;

        // Remove notification subscriptions.
        channel.RemoveAllListeners(@internal.DataProducerId);
        payloadChannel.RemoveAllListeners(@internal.DataProducerId);

        // TODO : Naming
        var reqData = new { dataProducerId = @internal.DataProducerId };

        // Fire and forget
        channel.Request("transport.closeDataProducer", @internal.TransportId, reqData)
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

        logger?.LogDebug("TransportClosedAsync() | DataProducer:{Id}", Id);

        Closed = true;

        // Remove notification subscriptions.
        channel.RemoveAllListeners(@internal.DataProducerId);
        payloadChannel.RemoveAllListeners(@internal.DataProducerId);

        _ = Emit("transportclose");

        // Emit observer event.
        _ = Observer.SafeEmit("close");
    }

    /// <summary>
    /// Dump DataProducer.
    /// </summary>
    public async Task<object> DumpAsync()
    {
        logger?.LogDebug("DumpAsync() | DataProducer:{Id}", Id);

        return (await channel.Request("dataProducer.dump", @internal.DataProducerId))!;
    }

    /// <summary>
    /// Get DataProducer stats. Return: DataProducerStat[]
    /// </summary>
    public async Task<List<DataProducerStat>> GetStatsAsync()
    {
        logger?.LogDebug("GetStatsAsync() | DataProducer:{Id}", Id);

        return (await channel.Request("dataProducer.getStats", @internal.DataProducerId) as List<DataProducerStat>)!;
    }

    public async Task SendAsync(object message, int? ppid)
    {
        if (message is not (string or byte[]))
        {
            throw new TypeError("message must be a string or a Buffer");
        }


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
            : ((byte[])message).Length > 0 ? 53 : 57;
        
        // Ensure we honor PPIDs.
        message = ppid switch
        {
            56 => " ",
            57 => new byte[1],
            _ => message
        };

        var notifData = ppid.Value.ToString();

        await payloadChannel.Request("dataProducer.send", @internal.DataProducerId, notifData, message);
    }
    
    private void HandleWorkerNotifications()
    {
        // No need to subscribe to any event.
    }
}