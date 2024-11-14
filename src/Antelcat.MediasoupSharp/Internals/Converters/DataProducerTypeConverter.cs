using Type = Antelcat.MediasoupSharp.FBS.DataProducer.Type;

namespace Antelcat.MediasoupSharp.Internals.Converters;

internal class DataProducerTypeConverter : EnumStringConverter<Type>
{
    protected override IEnumerable<(Type Enum, string Text)> Map()
    {
        yield return (Type.SCTP, "sctp");
        yield return (Type.DIRECT, "direct");
    }
}