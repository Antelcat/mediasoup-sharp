using Type = FBS.DataProducer.Type;

namespace MediasoupSharp.Internals.Converters;

internal class DataProducerTypeConverter : EnumStringConverter<Type>
{
    protected override IEnumerable<(Type Enum, string Text)> Map()
    {
        yield return (FBS.DataProducer.Type.SCTP, "sctp");
        yield return (FBS.DataProducer.Type.DIRECT, "direct");
    }
}