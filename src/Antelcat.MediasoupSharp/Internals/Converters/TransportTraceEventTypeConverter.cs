using Antelcat.MediasoupSharp.FBS.Transport;

namespace Antelcat.MediasoupSharp.Internals.Converters;

internal class TransportTraceEventTypeConverter : EnumStringConverter<TraceEventType>
{
    protected override IEnumerable<(TraceEventType Enum, string Text)> Map()
    {
        yield return (TraceEventType.PROBATION, "probation");
        yield return (TraceEventType.BWE, "bwe");
    }
}