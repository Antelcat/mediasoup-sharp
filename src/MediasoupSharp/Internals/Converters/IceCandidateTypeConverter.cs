using FBS.WebRtcTransport;

namespace MediasoupSharp.Internals.Converters;

internal class IceCandidateTypeConverter : EnumStringConverter<IceCandidateType>
{
    protected override IEnumerable<(IceCandidateType Enum, string Text)> Map()
    {
        yield return (IceCandidateType.HOST, "host");
    }
}