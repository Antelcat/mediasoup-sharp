namespace Antelcat.MediasoupSharp.Internals.Converters;

internal class RtpHeaderExtensionDirectionConverter : EnumStringConverter<RtpHeaderExtensionDirection>
{
    protected override IEnumerable<(RtpHeaderExtensionDirection Enum, string Text)> Map()
    {
        yield return (RtpHeaderExtensionDirection.SendReceive, "sendrecv");
        yield return (RtpHeaderExtensionDirection.SendOnly, "sendonly");
        yield return (RtpHeaderExtensionDirection.ReceiveOnly, "recvonly");
        yield return (RtpHeaderExtensionDirection.Inactive, "inactive");
    }
}