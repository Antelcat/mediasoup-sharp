using Antelcat.MediasoupSharp.FBS.Transport;

namespace Antelcat.MediasoupSharp.Internals.Converters;

internal class ProtocolConverter : EnumStringConverter<Protocol>
{
    protected override IEnumerable<(Protocol Enum, string Text)> Map()
    {
        yield return (Protocol.UDP, "udp");
        yield return (Protocol.TCP, "tcp");
    }
}