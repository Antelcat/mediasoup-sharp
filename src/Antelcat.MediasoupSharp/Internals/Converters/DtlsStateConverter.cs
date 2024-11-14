using Antelcat.MediasoupSharp.FBS.WebRtcTransport;

namespace Antelcat.MediasoupSharp.Internals.Converters;

internal class DtlsStateConverter: EnumStringConverter<DtlsState>
{
    protected override IEnumerable<(DtlsState Enum, string Text)> Map()
    {
        yield return (DtlsState.NEW, "new");
        yield return (DtlsState.CONNECTING, "connecting");
        yield return (DtlsState.CONNECTED, "connected");
        yield return (DtlsState.FAILED, "failed");
        yield return (DtlsState.CLOSED, "closed");
    }
}