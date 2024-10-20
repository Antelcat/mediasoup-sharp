using FBS.Consumer;

namespace MediasoupSharp.Internals.Converters;

internal class ConsumerTraceEventTypeConverter : EnumStringConverter<TraceEventType>
{
    protected override IEnumerable<(TraceEventType Enum, string Text)> Map()
    {
        yield return (TraceEventType.KEYFRAME, "keyframe");
        yield return (TraceEventType.FIR, "fir");
        yield return (TraceEventType.NACK, "nack");
        yield return (TraceEventType.PLI, "pli");
        yield return (TraceEventType.RTP, "rtp");
    }
}