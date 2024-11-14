using Antelcat.MediasoupSharp.FBS.WebRtcTransport;

namespace Antelcat.MediasoupSharp.Internals.Converters;

internal class IceStateConverter : EnumStringConverter<IceState>
{
    protected override IEnumerable<(IceState Enum, string Text)> Map()
    {
        yield return (IceState.NEW, "new");
        yield return (IceState.CONNECTED, "connected");
        yield return (IceState.COMPLETED, "completed");
        yield return (IceState.DISCONNECTED, "disconnected");
    }
}