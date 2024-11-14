using Antelcat.MediasoupSharp.FBS.Transport;

namespace Antelcat.MediasoupSharp.Internals.Converters;

internal class BweTypeConverter : EnumStringConverter<BweType>
{
    protected override IEnumerable<(BweType Enum, string Text)> Map()
    {
        yield return (BweType.TRANSPORT_CC, "transport-cc");
        yield return (BweType.REMB, "remb");
    }
}