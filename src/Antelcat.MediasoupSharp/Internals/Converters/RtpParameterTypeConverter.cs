using Type = FBS.RtpParameters.Type;

namespace Antelcat.MediasoupSharp.Internals.Converters;

internal class RtpParameterTypeConverter : EnumStringConverter<Type>
{
    protected override IEnumerable<(Type Enum, string Text)> Map()
    {
        yield return (FBS.RtpParameters.Type.SIMPLE, "simple");
        yield return (FBS.RtpParameters.Type.SIMULCAST, "simulcast");
        yield return (FBS.RtpParameters.Type.SVC, "svc");
        yield return (FBS.RtpParameters.Type.PIPE, "pipe");
    }
}