using static FBS.DataProducer.Type;
using Type = FBS.DataProducer.Type;

namespace Antelcat.MediasoupSharp.Internals.Converters;

internal class DataProducerTypeConverter : EnumStringConverter<Type>
{
    protected override IEnumerable<(Type Enum, string Text)> Map()
    {
        yield return (SCTP, "sctp");
        yield return (DIRECT, "direct");
    }
}