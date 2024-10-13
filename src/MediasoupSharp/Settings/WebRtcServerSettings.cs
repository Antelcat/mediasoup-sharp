using MediasoupSharp.FlatBuffers.Transport.T;

namespace MediasoupSharp.Settings;

public class WebRtcServerSettings
{
    public ListenInfoT[] ListenInfos { get; set; }
}
