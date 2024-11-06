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

    public static IEnumerable<IPAddress> GetLocalIPAddresses(this AddressFamily addressFamily) =>
        Dns.GetHostEntry(Dns.GetHostName()).AddressList
            .Where(m => m.AddressFamily == addressFamily);

    public static IPAddress? GetLocalIPv4IPAddress() => LocalIpV4Address ??= AddressFamily.InterNetwork
        .GetLocalIPAddresses()
        .FirstOrDefault(m => !IPAddress.IsLoopback(m));
    
    private static IPAddress? LocalIpV4Address { get; set; }

    [GeneratedRegex(@"^\d{1,3}[\.]\d{1,3}[\.]\d{1,3}[\.]\d{1,3}$", RegexOptions.Compiled)]
    private static partial Regex MyRegex();
}
