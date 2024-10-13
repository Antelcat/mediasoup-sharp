using FlatBuffers.SctpAssociation;
using MediasoupSharp.FlatBuffers.SctpParameters.T;

namespace MediasoupSharp.Transport;

public class TransportBaseData
{
    /// <summary>
    /// SCTP parameters.
    /// </summary>
    public SctpParametersT? SctpParameters { get; set; }

    /// <summary>
    /// Sctp state.
    /// </summary>
    public SctpState? SctpState { get; set; }
}
