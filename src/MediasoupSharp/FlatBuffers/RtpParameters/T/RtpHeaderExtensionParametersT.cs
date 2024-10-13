using FlatBuffers.RtpParameters;

namespace MediasoupSharp.FlatBuffers.RtpParameters.T;

public class RtpHeaderExtensionParametersT
{
    public RtpHeaderExtensionUri Uri { get; set; }

    public byte Id { get; set; }

    public bool Encrypt { get; set; }

    public List<ParameterT> Parameters { get; set; }
}
