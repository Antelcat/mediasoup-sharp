using System.Net;
using System.Net.Sockets;
using FBS.Transport;
using Antelcat.MediasoupSharp;
using Antelcat.MediasoupSharp.AspNetCore;
using Antelcat.MediasoupSharp.Internals.Extensions;
using Antelcat.MediasoupSharp.Settings;
using Antelcat.MediasoupSharp.Worker;
using IPAddressExtensions = Antelcat.MediasoupSharp.AspNetCore.Extensions.IPAddressExtensions;

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
            .AddSingleton<Mediasoup>()
            .AddTransient<Worker>()
            .AddTransient<WorkerNative>();
        return services;
    }

    private static void Configure(MediasoupOptions mediasoupOptions, IConfiguration configuration)
    {
        var confSettings       = configuration.GetSection(nameof(MediasoupOptions))
            .Get<MediasoupOptions>();
        var confWorkerSettings          = confSettings?.WorkerSettings;
        var confRouterSettings          = confSettings?.RouterOptions;
        var confWebRtcServerSettings    = confSettings?.WebRtcServerOptions;
        var confWebRtcTransportSettings = confSettings?.WebRtcTransportOptions;
        var confPlainTransportSettings  = confSettings?.PlainTransportOptions;

        if (confSettings != null) mediasoupOptions.NumWorkers ??= Environment.ProcessorCount;

        // WorkerSettings
        if (confWorkerSettings != null)
        {
            mediasoupOptions.WorkerSettings =
                mediasoupOptions.WorkerSettings.Apply(confWorkerSettings);
        }

        // RouterSettings
        if (confRouterSettings?.MediaCodecs.IsNullOrEmpty() is false)
        {
            mediasoupOptions.RouterOptions = confRouterSettings;

            // Fix RtpCodecCapabilities[x].Parameters 。从配置文件反序列化时将数字转换成了字符串，而 mediasoup-worker 有严格的数据类型验证，故这里进行修正。
            foreach (var codec in confRouterSettings.MediaCodecs.Where(static m => m.Parameters != null))
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
            mediasoupOptions.WebRtcServerOptions.ListenInfos = confWebRtcServerSettings.ListenInfos;

            // 如果没有设置 ListenInfos 则获取本机所有的 IPv4 地址进行设置。
            var listenInfos = mediasoupOptions.WebRtcServerOptions.ListenInfos;
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
                mediasoupOptions.WebRtcServerOptions.ListenInfos = listenInfosTemp.ToArray();
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
            mediasoupOptions.WebRtcTransportOptions =
                mediasoupOptions.WebRtcTransportOptions.Apply(confWebRtcTransportSettings); 

            // 如果没有设置 ListenInfos 则获取本机所有的 IPv4 地址进行设置。
            var listenAddresses = mediasoupOptions.WebRtcTransportOptions.ListenInfos;
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
                
                mediasoupOptions.WebRtcTransportOptions.ListenInfos = listenAddresses;
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
            mediasoupOptions.PlainTransportOptions = mediasoupOptions.PlainTransportOptions?.Apply(confPlainTransportSettings);

            var localIPv4IpAddress = IPAddressExtensions.GetLocalIPv4IPAddress()?.ToString() ??
                                     throw new ArgumentException("无法获取本机 IPv4 配置 PlainTransport。");

            var listenIp = mediasoupOptions.PlainTransportOptions?.ListenInfo;
            if (listenIp == null)
            {
                listenIp = new ListenInfoT
                {
                    Ip               = localIPv4IpAddress,
                    AnnouncedAddress = localIPv4IpAddress,
                    Flags            = new(),
                    PortRange        = new()
                };
                mediasoupOptions.PlainTransportOptions.ListenInfo = listenIp;
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