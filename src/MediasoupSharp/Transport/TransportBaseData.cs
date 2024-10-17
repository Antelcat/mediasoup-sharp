using FBS.SctpAssociation;
using FBS.SctpParameters;

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