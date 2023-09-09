﻿using MediasoupSharp.Consumer;
using MediasoupSharp.Exceptions;
using MediasoupSharp.Producer;
using MediasoupSharp.Transport;
using Microsoft.Extensions.Logging;

namespace MediasoupSharp.DirectTransport;

internal interface IDirectTransport
{
}

internal class DirectTransport<TDirectTransportAppData>
    : Transport.Transport<TDirectTransportAppData, DirectTransportEvents, DirectTransportObserverEvents>,
        IDirectTransport
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
    protected override void Close()
    {
        if (Closed)
        {
            return;
        }

        base.Close();
    }

    /// <summary>
    /// Router was closed.
    /// </summary>
    internal override void RouterClosed()
    {
        if (Closed)
        {
            return;
        }
        
        base.RouterClosed();
    }

    public new async Task<List<DirectTransportStat>> GetStatsAsync()
    {
        Logger?.LogDebug("getStats()");

        return (await Channel.Request("transport.getStats", Internal.TransportId) as List<DirectTransportStat>)!;
    }

    /// <summary>
    /// NO-OP method in DirectTransport.
    /// </summary>
    /// <param name="parameters"></param>
    /// <returns></returns>
    public override Task ConnectAsync(object parameters)
    {
        Logger?.LogDebug("ConnectAsync() | DiectTransport:{Id}", Id);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Set maximum incoming bitrate for receiving media.
    /// </summary>
    /// <param name="bitrate"></param>
    /// <returns></returns>
    public override Task<string> SetMaxIncomingBitrateAsync(int bitrate)
    {
        throw new NotSupportedException(
            "SetMaxIncomingBitrateAsync() not implemented in DirectTransport");
    }

    /// <summary>
    /// Set maximum outgoing bitrate for sending media.
    /// </summary>
    /// <param name="bitrate"></param>
    /// <returns></returns>
    public override Task<string> SetMaxOutgoingBitrateAsync(int bitrate)
    {
        throw new NotSupportedException(
            "SetMaxOutgoingBitrateAsync() is not implemented in DirectTransport");
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="bitrate"></param>
    /// <returns></returns>
    public override Task SetMinOutgoingBitrate(int bitrate)
    {
        throw new NotSupportedException(
            "setMinOutgoingBitrate() not implemented in DirectTransport");
    }

    public void SendRtcp(byte[] rtcpPacket)
    {
        PayloadChannel.Notify(
            "transport.sendRtcp", Internal.TransportId, null, rtcpPacket);
    }

    private void HandleWorkerNotifications()
    {
        Channel.On(Internal.TransportId, async args =>
        {
            var @event = args![0] as string;
            var data   = args[1] as dynamic;
            switch (@event)
            {
                case "trace":
                {
                    var trace = (data as TransportTraceEventData)!;

                    await SafeEmit("trace", trace);

                    // Emit observer event.
                    await Observer.SafeEmit("trace", trace);

                    break;
                }

                default:
                {
                    Logger?.LogError("ignoring unknown event {E}", @event);

                    break;
                }
            }
        });

        PayloadChannel.On(
            Internal.TransportId, async args =>
            {
                var @event  = args![0] as string;
                var data    = args[1] as dynamic;
                var payload = args[2] as byte[];
                switch (@event)
                {
                    case "rtcp":
                    {
                        if (Closed)
                        {
                            break;
                        }

                        var packet = payload!;

                        await SafeEmit("rtcp", packet);

                        break;
                    }

                    default:
                    {
                        Logger?.LogError("ignoring unknown event {E}", @event);
                        break;
                    }
                }
            });
    }
}