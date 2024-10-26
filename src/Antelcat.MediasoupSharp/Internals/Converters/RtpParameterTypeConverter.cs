using static FBS.RtpParameters.Type;
using Type = FBS.RtpParameters.Type;

namespace Antelcat.MediasoupSharp.Internals.Converters;

internal class RtpParameterTypeConverter : EnumStringConverter<Type>
{
    protected override IEnumerable<(Type Enum, string Text)> Map()
    {
        yield return (SIMPLE, "simple");
        yield return (SIMULCAST, "simulcast");
        yield return (SVC, "svc");
        yield return (PIPE, "pipe");
    }
}