using FBS.WebRtcTransport;

namespace Antelcat.MediasoupSharp.Internals.Converters;

internal class DtlsRoleConverter : EnumStringConverter<DtlsRole>
{
    protected override IEnumerable<(DtlsRole Enum, string Text)> Map()
    {
        yield return (DtlsRole.AUTO, "auto");
        yield return (DtlsRole.CLIENT, "client");
        yield return (DtlsRole.SERVER, "server");
    }
}