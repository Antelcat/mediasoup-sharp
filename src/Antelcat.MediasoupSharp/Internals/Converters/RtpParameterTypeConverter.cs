using Type = Antelcat.MediasoupSharp.FBS.RtpParameters.Type;

namespace Antelcat.MediasoupSharp.Internals.Converters;

internal class RtpParameterTypeConverter : EnumStringConverter<Type>
{
    protected override IEnumerable<(Type Enum, string Text)> Map()
    {
        yield return (Type.SIMPLE, "simple");
        yield return (Type.SIMULCAST, "simulcast");
        yield return (Type.SVC, "svc");
        yield return (Type.PIPE, "pipe");
    }
}