using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using Antelcat.MediasoupSharp;
using Antelcat.MediasoupSharp.AspNetCore;
using Antelcat.MediasoupSharp.AspNetCore.Extensions;
using Antelcat.MediasoupSharp.Internals.Extensions;
using Antelcat.MediasoupSharp.FBS.Transport;
using IPAddressExtensions = Antelcat.MediasoupSharp.AspNetCore.Extensions.IPAddressExtensions;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class MediasoupServiceCollectionExtensions
{

    private static void Correct<T>(this WorkerSettings<T> options)
    {
    }

    private static void Correct<T>(this RouterOptions<T> options)
    {
        foreach (var codec in options.MediaCodecs.Where(static m => m.Parameters != null))
        {
            var param = codec.Parameters.NotNull();
            foreach (var key in param.Keys.ToArray())
            {
                var value = param[key];
                if (int.TryParse(value?.ToString(), out var intValue))
                {
                    param[key] = intValue;
                }
            }
        }
    }

    private static void Correct<T>(this WebRtcServerOptions<T> options)
    {
        options.ListenInfos = options.ListenInfos.Correct();
    }

    private static void Correct<T>(this WebRtcTransportOptions<T> options)
    {
        options.ListenInfos = options.ListenInfos.Correct();
    }

    private static void Correct<T>(this PlainTransportOptions<T> options)
    {
        options.ListenInfo.Correct();
    }

    private static ListenInfoT[] Correct(this ListenInfoT[] listenInfos)
    {
        foreach (var listenInfo in listenInfos)
        {
            listenInfo.Correct();
        }

        if (listenInfos.Length != 0) return listenInfos;

        var create =
            (Environment.GetEnvironmentVariable("MEDIASOUP_ANNOUNCED_IP") is { } announcedId
                ? (string[]) [announcedId]
                : AddressFamily.InterNetwork.GetLocalIPAddresses()
                    .Where(static m => !Equals(m, IPAddress.Loopback))
                    .Select(x => x.ToString()))
            .SelectMany(static x => (ListenInfoT[])
            [
                new ListenInfoT
                {
                    Ip               = x,
                    Port             = 44444,
                    Protocol         = Protocol.TCP,
                    AnnouncedAddress = x,
                    Flags            = new(),
                    PortRange        = new()
                },
                new ListenInfoT
                {
                    Ip               = x,
                    Port             = 44444,
                    Protocol         = Protocol.UDP,
                    AnnouncedAddress = x,
                    Flags            = new(),
                    PortRange        = new()
                }
            ])
            .ToArray();

        if (create.Length == 0) throw new OperationCanceledException("Cannot get local ip address");

        return create;
    }

    private static void Correct(this ListenInfoT listenInfo)
    {
        listenInfo.Flags     ??= new();
        listenInfo.PortRange ??= new();
        listenInfo.Ip        ??= "0.0.0.0";
        listenInfo.AnnouncedAddress ??= IPAddressExtensions.GetLocalIPv4IPAddress()?.ToString()
                                        ?? throw new OperationCanceledException("Cannot get local ip address");
    }

}