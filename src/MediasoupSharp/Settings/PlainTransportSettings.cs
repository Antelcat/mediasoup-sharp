using MediasoupSharp.FlatBuffers.Transport.T;

namespace MediasoupSharp.Settings;

public class PlainTransportSettings
{
    public ListenInfoT ListenInfo { get; set; }

    public uint MaxSctpMessageSize { get; set; }
}
