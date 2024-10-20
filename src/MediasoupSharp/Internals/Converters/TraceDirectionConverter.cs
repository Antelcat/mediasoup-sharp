using FBS.Common;

namespace MediasoupSharp.Internals.Converters;

internal class TraceDirectionConverter : EnumStringConverter<TraceDirection>
{
    protected override IEnumerable<(TraceDirection Enum, string Text)> Map()
    {
        yield return (TraceDirection.DIRECTION_IN, "in");
        yield return (TraceDirection.DIRECTION_OUT, "out");
    }
}