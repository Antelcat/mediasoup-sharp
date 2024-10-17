using MediasoupSharp.SctpParameters;

namespace MediasoupSharp.ClientRequest;

public class CreateWebRtcTransportRequest
{
    public bool ForceTcp { get; set; }

    public SctpCapabilities? SctpCapabilities { get; set; }
}