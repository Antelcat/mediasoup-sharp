using FBS.Transport;

namespace MediasoupSharp.Settings;

public class PlainTransportSettings
{
    public ListenInfoT ListenInfo { get; set; }

    public uint MaxSctpMessageSize { get; set; }
}