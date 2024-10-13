using MediasoupSharp.FlatBuffers.Transport.T;

namespace MediasoupSharp.FlatBuffers.Worker.T;

public class CreateWebRtcServerRequestT
{
    public string WebRtcServerId { get; set; }

    public List<ListenInfoT> ListenInfos { get; set; }
}