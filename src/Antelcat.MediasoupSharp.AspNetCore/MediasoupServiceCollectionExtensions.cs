using System.Net;
using System.Net.Sockets;
using Antelcat.MediasoupSharp;
using Antelcat.MediasoupSharp.AspNetCore;
using Antelcat.MediasoupSharp.Internals.Extensions;
using FBS.Transport;
using IPAddressExtensions = Antelcat.MediasoupSharp.AspNetCore.Extensions.IPAddressExtensions;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class MediasoupServiceCollectionExtensions
{
    public static IServiceCollection AddMediasoup<T>(this IServiceCollection services,
                                                     Action<MediasoupOptionsContext<T>>? configure = null) =>
        AddMediasoup(services, MediasoupOptionsContext<T>.Default, configure);

    public static IServiceCollection AddMediasoup<T>(this IServiceCollection services,
                                                     MediasoupOptionsContext<T> mediasoupOptions,
                                                     Action<MediasoupOptionsContext<T>>? configure = null)
    {
        services
            .AddSingleton<MediasoupOptionsContext<T>>(x =>
            {
                var conf = x.GetService<IConfiguration>()?.GetSection(nameof(MediasoupOptionsContext<T>)).Get<MediasoupOptionsContext<T>>();
                if (conf != null) Configure(mediasoupOptions, conf);
                configure?.Invoke(mediasoupOptions);
                return mediasoupOptions;
            })
            .AddSingleton<Mediasoup>();
        return services;
    }

    public static void Configure<T>(MediasoupOptionsContext<T> defaultOptions, MediasoupOptionsContext<T> userOptions)
    {
        var userWorkerSettings         = userOptions.WorkerSettings;
        var userRouterOptions          = userOptions.RouterOptions;
        var userWebRtcServerOptions    = userOptions.WebRtcServerOptions;
        var userWebRtcTransportOptions = userOptions.WebRtcTransportOptions;
        var userPlainTransportOptions  = userOptions.PlainTransportOptions;

        defaultOptions.NumWorkers = userOptions.NumWorkers ??= Environment.ProcessorCount;

        // WorkerOptions
        if (userWorkerSettings != null)
        {
            defaultOptions.WorkerSettings = defaultOptions.WorkerSettings!.Apply(userWorkerSettings);
        }

        // RouterOptions
        if (userRouterOptions?.MediaCodecs.IsNullOrEmpty() is false)
        {
            defaultOptions.RouterOptions = userRouterOptions;

            foreach (var codec in userRouterOptions.MediaCodecs.Where(static m => m.Parameters != null))
            {
                foreach (var key in codec.Parameters!.Keys.ToArray())
                {
                    var value = codec.Parameters[key];
                    if (int.TryParse(value?.ToString(), out var intValue))
                    {
                        codec.Parameters[key] = intValue;
                    }
                }
            }
        }

        // WebRtcServerOptions
        if (userWebRtcServerOptions != null)
        {
            defaultOptions.WebRtcServerOptions!.ListenInfos = userWebRtcServerOptions.ListenInfos
                .Select(static x =>
                {
                    x.Flags     ??= new();
                    x.PortRange ??= new();
                    return x;
                }).ToArray();

            var listenInfos = defaultOptions.WebRtcServerOptions.ListenInfos;
            if (listenInfos.IsNullOrEmpty())
            {
                var localIPv4IpAddresses = IPAddressExtensions.GetLocalIPAddresses(AddressFamily.InterNetwork)
                    .Where(static m => !Equals(m, IPAddress.Loopback));

                var listenInfosTemp = (from ip in localIPv4IpAddresses
                    let ipString = ip.ToString()
                    select new ListenInfoT
                    {
                        Ip               = ipString,
                        Port             = 44444,
                        Protocol         = Protocol.TCP,
                        AnnouncedAddress = ipString,
                        Flags            = new(),
                        PortRange        = new()
                    }).ToList();

                if (listenInfosTemp.IsNullOrEmpty())
                {
                    throw new ArgumentException("无法获取本机 IPv4 配置 WebRtcServer");
                }

                listenInfosTemp.AddRange(listenInfosTemp.Select(static m => new ListenInfoT
                {
                    Ip               = m.Ip,
                    Port             = m.Port,
                    Protocol         = Protocol.UDP,
                    AnnouncedAddress = m.AnnouncedAddress,
                    Flags            = m.Flags     ?? new(),
                    PortRange        = m.PortRange ?? new()
                }));
                defaultOptions.WebRtcServerOptions.ListenInfos = listenInfosTemp.ToArray();
            }
            else
            {
                var localIPv4IpAddress = IPAddressExtensions.GetLocalIPv4IPAddress() ??
                                         throw new ArgumentException("无法获取本机 IPv4 配置 WebRtcServer。");

                foreach (var listenIp in listenInfos)
                {
                    if (listenIp.AnnouncedAddress.IsNullOrWhiteSpace())
                    {
                        listenIp.AnnouncedAddress = listenIp.Ip == IPAddress.Any.ToString()
                            ? localIPv4IpAddress.ToString()
                            : listenIp.Ip;
                    }
                }
            }
        }

        // WebRtcTransportOptions
        if (userWebRtcTransportOptions != null)
        {
            defaultOptions.WebRtcTransportOptions =
                defaultOptions.WebRtcTransportOptions!.Apply(userWebRtcTransportOptions);

            var listenAddresses = defaultOptions.WebRtcTransportOptions.ListenInfos;
            if (listenAddresses.IsNullOrEmpty())
            {
                var localIPv4IpAddresses = IPAddressExtensions.GetLocalIPAddresses(AddressFamily.InterNetwork)
                    .Where(static m => !Equals(m, IPAddress.Loopback));

                listenAddresses = (from ip in localIPv4IpAddresses
                    let ipString = ip.ToString()
                    select new ListenInfoT
                    {
                        Ip               = ipString,
                        AnnouncedAddress = ipString,
                        Flags            = new(),
                        PortRange        = new()
                    }).ToArray();

                if (listenAddresses.IsNullOrEmpty())
                {
                    throw new ArgumentException("无法获取本机 IPv4 配置 WebRtcTransport");
                }

                defaultOptions.WebRtcTransportOptions.ListenInfos = listenAddresses;
            }
            else
            {
                var localIPv4IpAddress = IPAddressExtensions.GetLocalIPv4IPAddress() ??
                                         throw new ArgumentException("无法获取本机 IPv4 配置 WebRtcTransport。");

                foreach (var listenAddress in listenAddresses)
                {
                    if (listenAddress.AnnouncedAddress.IsNullOrWhiteSpace())
                    {
                        listenAddress.AnnouncedAddress = listenAddress.Ip == IPAddress.Any.ToString()
                            ? localIPv4IpAddress.ToString()
                            : listenAddress.Ip;
                    }
                }
            }
        }

        // PlainTransportOptions
        if (userPlainTransportOptions != null)
        {
            defaultOptions.PlainTransportOptions =
                defaultOptions.PlainTransportOptions?.Apply(userPlainTransportOptions);

            var localIPv4IpAddress = IPAddressExtensions.GetLocalIPv4IPAddress()?.ToString() ??
                                     throw new ArgumentException("无法获取本机 IPv4 配置 PlainTransport。");

            var listenIp = defaultOptions.PlainTransportOptions?.ListenInfo;
            if (listenIp == null)
            {
                listenIp = new ListenInfoT
                {
                    Ip               = localIPv4IpAddress,
                    AnnouncedAddress = localIPv4IpAddress,
                    Flags            = new(),
                    PortRange        = new()
                };
                defaultOptions.PlainTransportOptions!.ListenInfo = listenIp;
            }
            else if (listenIp.AnnouncedAddress.IsNullOrWhiteSpace())
            {
                listenIp.AnnouncedAddress = listenIp.Ip == IPAddress.Any.ToString()
                    ? localIPv4IpAddress
                    : listenIp.Ip;
            }
        }
    }
}