using FBS.Transport;

namespace MediasoupSharp.Settings;

public record PlainTransportSettings
{
    public ListenInfoT? ListenInfo { get; set; }

    public uint? MaxSctpMessageSize { get; set; }
}