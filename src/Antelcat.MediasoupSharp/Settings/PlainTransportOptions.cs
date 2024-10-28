using FBS.Transport;

namespace Antelcat.MediasoupSharp.Settings;

public record PlainTransportOptions
{
    public ListenInfoT? ListenInfo { get; set; }

    public uint? MaxSctpMessageSize { get; set; }
}