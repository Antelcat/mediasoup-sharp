using FBS.WebRtcTransport;

namespace Antelcat.MediasoupSharp.Internals.Converters;

internal class IceRoleConverter : EnumStringConverter<IceRole>
{
    protected override IEnumerable<(IceRole Enum, string Text)> Map()
    {
        yield return (IceRole.CONTROLLED, "controlled");
        yield return (IceRole.CONTROLLING, "controlling");
    }
}