using System.Net;
using System.Net.Sockets;
using FBS.Transport;
using MediasoupSharp;
using MediasoupSharp.AspNetCore;
using MediasoupSharp.Settings;
using MediasoupSharp.Worker;
using MediasoupSharp.Extensions;
using MediasoupSharp.Internals.Extensions;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class MediasoupServiceCollectionExtensions
{
    public static IServiceCollection AddMediasoup(this IServiceCollection services,
                                                  Action<MediasoupOptions>? configure = null) =>
        AddMediasoup(services, MediasoupOptions.Default, configure);

    public static IServiceCollection AddMediasoup(this IServiceCollection services,
                                                  MediasoupOptions mediasoupOptions,
                                                  Action<MediasoupOptions>? configure = null)
    {
        services
            .AddSingleton<MediasoupOptions>(x =>
            {
                var conf = x.GetService<IConfiguration>();
                if (conf != null) Configure(mediasoupOptions, conf);
                configure?.Invoke(mediasoupOptions);
                return mediasoupOptions;
            })
            .AddSingleton<MediasoupServer>()
            .AddTransient<Worker>()
            .AddTransient<WorkerNative>();
        return services;
    }

    private static void Configure(MediasoupOptions mediasoupOptions, IConfiguration configuration)
    {
        var confStartupSettings = configuration.GetSection(nameof(MediasoupStartupSettings))
            .Get<MediasoupStartupSettings>();
        var confSettings       = configuration.GetSection(nameof(MediasoupSettings))
            .Get<MediasoupSettings>();
        var confWorkerSettings          = confSettings?.WorkerSettings;
        var confRouterSettings          = confSettings?.RouterSettings;
        var confWebRtcServerSettings    = confSettings?.WebRtcServerSettings;
        var confWebRtcTransportSettings = confSettings?.WebRtcTransportSettings;
        var confPlainTransportSettings  = confSettings?.PlainTransportSettings;

        if (confStartupSettings != null)
        {
            mediasoupOptions.MediasoupStartupSettings.Apply(mediasoupOptions.MediasoupStartupSettings);
            mediasoupOptions.MediasoupStartupSettings.NumberOfWorkers
                = mediasoupOptions.MediasoupStartupSettings.NumberOfWorkers is null or <= 0
                    ? Environment.ProcessorCount
                    : mediasoupOptions.MediasoupStartupSettings.NumberOfWorkers;
        }

        // WorkerSettings
        if (confWorkerSettings != null)
        {
            mediasoupOptions.MediasoupSettings.WorkerSettings =
                mediasoupOptions.MediasoupSettings.WorkerSettings.Apply(confWorkerSettings);
        }

        // RouterSettings
        if (confRouterSettings?.RtpCodecCapabilities.IsNullOrEmpty() is false)
        {
            mediasoupOptions.MediasoupSettings.RouterSettings = confRouterSettings;

            // Fix RtpCodecCapabilities[x].Parameters 。从配置文件反序列化时将数字转换成了字符串，而 mediasoup-worker 有严格的数据类型验证，故这里进行修正。
            foreach (var codec in confRouterSettings.RtpCodecCapabilities.Where(static m => m.Parameters != null))
            {
                foreach (var key in codec.Parameters!.Keys.ToArray())
                {
                    var value = codec.Parameters[key];
                    if (value != null && int.TryParse(value.ToString(), out var intValue))
                    {
                        codec.Parameters[key] = intValue;
                    }
                }
            }
        }

        // WebRtcServerSettings
        if (confWebRtcServerSettings != null)
        {
            mediasoupOptions.MediasoupSettings.WebRtcServerSettings.ListenInfos = confWebRtcServerSettings.ListenInfos;

            // 如果没有设置 ListenInfos 则获取本机所有的 IPv4 地址进行设置。
            var listenInfos = mediasoupOptions.MediasoupSettings.WebRtcServerSettings.ListenInfos;
            if (listenInfos.IsNullOrEmpty())
            {
                var localIPv4IpAddresses = IPAddressExtensions.GetLocalIPAddresses(AddressFamily.InterNetwork)
                    .Where(m => !Equals(m, IPAddress.Loopback));

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
                    throw new ArgumentException("无法获取本机 IPv4 配置 WebRtcServer。");
                }

                listenInfosTemp.AddRange(listenInfosTemp.Select(static m => new ListenInfoT
                {
                    Ip               = m.Ip,
                    Port             = m.Port,
                    Protocol         = Protocol.UDP,
                    AnnouncedAddress = m.AnnouncedAddress,
                    Flags            = m.Flags,
                    PortRange        = m.PortRange
                }));
                mediasoupOptions.MediasoupSettings.WebRtcServerSettings.ListenInfos = listenInfosTemp.ToArray();
            }
            else
            {
                var localIPv4IpAddress = IPAddressExtensions.GetLocalIPv4IPAddress() ??
                                         throw new ArgumentException("无法获取本机 IPv4 配置 WebRtcServer。");

                foreach (var listenIp in listenInfos)
                {
                    if (listenIp.AnnouncedAddress.IsNullOrWhiteSpace())
                    {
                        // 如果没有设置 AnnouncedAddress：
                        // 如果 Ip 属性的值不是 Any 则赋值为 Ip 属性的值，否则取本机的任意一个 IPv4 地址进行设置。(注意：可能获取的并不是正确的 IP)
                        listenIp.AnnouncedAddress = listenIp.Ip == IPAddress.Any.ToString()
                            ? localIPv4IpAddress.ToString()
                            : listenIp.Ip;
                    }
                }
            }
        }

        // WebRtcTransportSettings
        if (confWebRtcTransportSettings != null)
        {
            mediasoupOptions.MediasoupSettings.WebRtcTransportSettings =
                mediasoupOptions.MediasoupSettings.WebRtcTransportSettings.Apply(confWebRtcTransportSettings); 

            // 如果没有设置 ListenInfos 则获取本机所有的 IPv4 地址进行设置。
            var listenAddresses = mediasoupOptions.MediasoupSettings.WebRtcTransportSettings.ListenInfos;
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
                    throw new ArgumentException("无法获取本机 IPv4 配置 WebRtcTransport。");
                }
                
                mediasoupOptions.MediasoupSettings.WebRtcTransportSettings.ListenInfos = listenAddresses;
            }
            else
            {
                var localIPv4IpAddress = IPAddressExtensions.GetLocalIPv4IPAddress() ??
                                         throw new ArgumentException("无法获取本机 IPv4 配置 WebRtcTransport。");

                foreach (var listenAddress in listenAddresses)
                {
                    if (listenAddress.AnnouncedAddress.IsNullOrWhiteSpace())
                    {
                        // 如果没有设置 AnnouncedAddress：
                        // 如果 Ip 属性的值不是 Any 则赋值为 Ip 属性的值，否则取本机的任意一个 IPv4 地址进行设置。(注意：可能获取的并不是正确的 IP)
                        listenAddress.AnnouncedAddress = listenAddress.Ip == IPAddress.Any.ToString()
                            ? localIPv4IpAddress.ToString()
                            : listenAddress.Ip;
                    }
                }
            }
        }

        // PlainTransportSettings
        if (confPlainTransportSettings != null)
        {
            mediasoupOptions.MediasoupSettings.PlainTransportSettings =
                mediasoupOptions.MediasoupSettings.PlainTransportSettings.Apply(confPlainTransportSettings);

            var localIPv4IpAddress = IPAddressExtensions.GetLocalIPv4IPAddress()?.ToString() ??
                                     throw new ArgumentException("无法获取本机 IPv4 配置 PlainTransport。");

            var listenIp = mediasoupOptions.MediasoupSettings.PlainTransportSettings.ListenInfo;
            if (listenIp == null)
            {
                listenIp = new ListenInfoT
                {
                    Ip               = localIPv4IpAddress,
                    AnnouncedAddress = localIPv4IpAddress,
                    Flags            = new(),
                    PortRange        = new()
                };
                mediasoupOptions.MediasoupSettings.PlainTransportSettings.ListenInfo = listenIp;
            }
            else if (listenIp.AnnouncedAddress.IsNullOrWhiteSpace())
            {
                // 如果没有设置 AnnouncedAddress：
                // 如果 Ip 属性的值不是 Any 则赋值为 Ip 属性的值，否则取本机的任意一个 IPv4 地址进行设置。(注意：可能获取的并不是正确的 IP)
                listenIp.AnnouncedAddress = listenIp.Ip == IPAddress.Any.ToString()
                    ? localIPv4IpAddress
                    : listenIp.Ip;
            }
        }
    }
}