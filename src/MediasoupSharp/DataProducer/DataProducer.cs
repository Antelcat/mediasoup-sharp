using System.Text;
using MediasoupSharp.Exceptions;
using MediasoupSharp.SctpParameters;
using Microsoft.Extensions.Logging;

namespace MediasoupSharp.DataProducer;

internal class DataProducer<TDataProducerAppData> : EnhancedEventEmitter<DataProducerEvents>
{
    /// <summary>
    /// Logger.
    /// </summary>
    private readonly ILogger logger;
    

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
    public TDataProducerAppData AppData { get; set; }

    /// <summary>
    /// Observer instance.
    /// </summary>
    private readonly EnhancedEventEmitter<DataProducerObserverEvents> observer;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="loggerFactory"></param>
    /// <param name="internal"></param>
    /// <param name="data"></param>
    /// <param name="channel"></param>
    /// <param name="payloadChannel"></param>
    /// <param name="appData"></param>
    public DataProducer(ILoggerFactory loggerFactory,
        DataProducerInternal @internal,
        DataProducerData data,
        Channel.Channel channel,
        PayloadChannel.PayloadChannel payloadChannel,
        TDataProducerAppData? appData
    ) : base(loggerFactory.CreateLogger("DataProducer"))
    {
        logger = loggerFactory.CreateLogger(GetType());

        this.@internal = @internal;
        this.data = data;
        this.channel = channel;
        this.payloadChannel = payloadChannel;
        AppData = appData ?? default!;
        observer = new EnhancedEventEmitter<DataProducerObserverEvents>(logger);
        
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

        logger.LogDebug("CloseAsync() | DataProducer:{Id}", Id);

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
        _ = observer.SafeEmit("close");

    }

    /// <summary>
    /// Transport was closed.
    /// </summary>
    public async Task TransportClosedAsync()
    {
        if (Closed)
        {
            return;
        }

        logger.LogDebug("TransportClosedAsync() | DataProducer:{Id}", Id);

        Closed = true;

        // Remove notification subscriptions.
        channel.RemoveAllListeners(@internal.DataProducerId);
        payloadChannel.RemoveAllListeners(@internal.DataProducerId);

        _ = Emit("transportclose");

        // Emit observer event.
        _ = observer.SafeEmit("close");
    }

    /// <summary>
    /// Dump DataProducer.
    /// </summary>
    public async Task<object> DumpAsync()
    {
        logger.LogDebug("DumpAsync() | DataProducer:{Id}", Id);

        return (await channel.Request("dataProducer.dump", @internal.DataProducerId))!;
    }

    /// <summary>
    /// Get DataProducer stats. Return: DataProducerStat[]
    /// </summary>
    public async Task<List<DataProducerStat>> GetStatsAsync()
    {
        logger.LogDebug("GetStatsAsync() | DataProducer:{Id}", Id);

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