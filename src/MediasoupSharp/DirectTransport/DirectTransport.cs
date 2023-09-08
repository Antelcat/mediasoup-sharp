using MediasoupSharp.Consumer;
using MediasoupSharp.Exceptions;
using MediasoupSharp.Producer;


namespace MediasoupSharp.DirectTransport;

internal class DirectTransport<TDirectTransportAppData> 
    : Transport.Transport<TDirectTransportAppData,DirectTransportEvents,DirectTransportObserverEvents>
{
    
    private readonly DirectTransportData data;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="options"></param>
    public DirectTransport(
        DirectTransportConstructorOptions<TDirectTransportAppData> options
    ) : base(options)
    {
        data = new DirectTransportData();

        HandleWorkerNotifications();
    }

    /// <summary>
    /// Close the DirectTransport.
    /// </summary>
    /// <returns></returns>
    public void Close()
    {
        if (Closed)
        {
            return;
        }
    }

    /// <summary>
    /// Router was closed.
    /// </summary>
    protected override Task OnRouterClosedAsync()
    {
        // Do nothing
        return Task.CompletedTask;
    }

    public override Task<List<object>> GetStatsAsync()
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// NO-OP method in DirectTransport.
    /// </summary>
    /// <param name="parameters"></param>
    /// <returns></returns>
    public override Task ConnectAsync(object parameters)
    {
        Logger?.LogDebug($"ConnectAsync() | DiectTransport:{TransportId}");
        return Task.CompletedTask;
    }

    /// <summary>
    /// Set maximum incoming bitrate for receiving media.
    /// </summary>
    /// <param name="bitrate"></param>
    /// <returns></returns>
    public override Task<string> SetMaxIncomingBitrateAsync(int bitrate)
    {
        Logger?.LogError("SetMaxIncomingBitrateAsync() | DiectTransport:{Id} Bitrate:{Bitrate}", Id, bitrate);
        throw new NotImplementedException("SetMaxIncomingBitrateAsync() not implemented in DirectTransport");
    }

    /// <summary>
    /// Set maximum outgoing bitrate for sending media.
    /// </summary>
    /// <param name="bitrate"></param>
    /// <returns></returns>
    public override Task<string> SetMaxOutgoingBitrateAsync(int bitrate)
    {
        Logger?.LogError($"SetMaxOutgoingBitrateAsync() | DiectTransport:{TransportId} Bitrate:{bitrate}");
        throw new NotImplementedException("SetMaxOutgoingBitrateAsync is not implemented in DirectTransport");
    }

    /// <summary>
    /// Create a Producer.
    /// </summary>
    public override Task<Producer.Producer> ProduceAsync(ProducerOptions producerOptions)
    {
        Logger?.LogError($"ProduceAsync() | DiectTransport:{TransportId}");
        throw new NotImplementedException("ProduceAsync() is not implemented in DirectTransport");
    }

    /// <summary>
    /// Create a Consumer.
    /// </summary>
    /// <param name="consumerOptions"></param>
    /// <returns></returns>
    public override Task<Consumer.Consumer> ConsumeAsync(ConsumerOptions consumerOptions)
    {
        Logger?.LogError($"ConsumeAsync() | DiectTransport:{TransportId}");
        throw new NotImplementedException("ConsumeAsync() not implemented in DirectTransport");
    }

    public async Task SendRtcpAsync(byte[] rtcpPacket)
    {
        await using (await CloseLock.ReadLockAsync())
        {
            if (Closed)
            {
                throw new InvalidStateException("Transport closed");
            }

            await PayloadChannel.NotifyAsync("transport.sendRtcp", Internal.TransportId, null, rtcpPacket);
        }
    }

    #region Event Handlers

    private void HandleWorkerNotifications()
    {
        Channel.MessageEvent += OnChannelMessage;
        PayloadChannel.MessageEvent += OnPayloadChannelMessage;
    }

    private void OnChannelMessage(string targetId, string @event, string? data)
    {
        if (targetId != Internal.TransportId)
        {
            return;
        }

        switch (@event)
        {
            case "trace":
            {
                var trace = data!.Deserialize<TransportTraceEventData>()!;

                Emit("trace", trace);

                // Emit observer event.
                Observer.Emit("trace", trace);

                break;
            }

            default:
            {
                Logger?.LogError(
                    $"OnChannelMessage() | DiectTransport:{TransportId} Ignoring unknown event{@event}");
                break;
            }
        }
    }

    private void OnPayloadChannelMessage(string targetId, string @event, string? data, ArraySegment<byte> payload)
    {
        if (targetId != Internal.TransportId)
        {
            return;
        }

        switch (@event)
        {
            case "rtcp":
            {
                _ = Emit("rtcp", payload);

                break;
            }

            default:
            {
                Logger?.LogError($"Ignoring unknown event \"{@event}\"");
                break;
            }
        }
    }

    #endregion Event Handlers
}