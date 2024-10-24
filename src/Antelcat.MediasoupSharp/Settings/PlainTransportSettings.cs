using FBS.Transport;

namespace Antelcat.MediasoupSharp.Settings;

public record PlainTransportSettings
{
    public ListenInfoT? ListenInfo { get; set; }

    public uint? MaxSctpMessageSize { get; set; }
}