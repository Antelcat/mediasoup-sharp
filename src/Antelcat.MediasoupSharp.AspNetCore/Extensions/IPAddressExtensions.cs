using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;

namespace Antelcat.MediasoupSharp.AspNetCore.Extensions;

/// <summary>
/// IPAddress 扩展方法
/// </summary>
internal static partial class IPAddressExtensions
{
    private static readonly Regex IpV4Regex = MyRegex();


    /// <summary>
    /// 获取本机 IP 地址
    /// </summary>
    public static IEnumerable<IPAddress> GetLocalIPAddresses(AddressFamily? addressFamily = null)
    {
        return Dns.GetHostEntry(Dns.GetHostName()).AddressList
            .Where(m => addressFamily == null || m.AddressFamily == addressFamily);
    }

    /// <summary>
    /// 获取一个本机的 IPv4 地址
    /// </summary>
    public static IPAddress? GetLocalIPv4IPAddress()
    {
        return GetLocalIPAddresses(AddressFamily.InterNetwork).FirstOrDefault(m => !IPAddress.IsLoopback(m));
    }

    [GeneratedRegex(@"^\d{1,3}[\.]\d{1,3}[\.]\d{1,3}[\.]\d{1,3}$", RegexOptions.Compiled)]
    private static partial Regex MyRegex();
}
