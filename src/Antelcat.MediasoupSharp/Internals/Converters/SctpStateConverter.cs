using Antelcat.MediasoupSharp.FBS.SctpAssociation;

namespace Antelcat.MediasoupSharp.Internals.Converters;

internal class SctpStateConverter : EnumStringConverter<SctpState>
{
    protected override IEnumerable<(SctpState Enum, string Text)> Map()
    {
        yield return (SctpState.NEW, "new");
        yield return (SctpState.CONNECTING, "connecting");
        yield return (SctpState.CONNECTED, "connected");
        yield return (SctpState.FAILED, "failed");
        yield return (SctpState.CLOSED, "closed");
    }
}