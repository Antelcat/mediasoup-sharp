using FBS.WebRtcTransport;

namespace MediasoupSharp.Internals.Converters;

internal class IceCandidateTcpTypeConverter : EnumStringConverter<IceCandidateTcpType>
{
    protected override IEnumerable<(IceCandidateTcpType Enum, string Text)> Map()
    {
        yield return (IceCandidateTcpType.PASSIVE, "passive");
    }
}