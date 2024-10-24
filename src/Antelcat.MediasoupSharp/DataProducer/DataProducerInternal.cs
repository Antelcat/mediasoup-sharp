using Antelcat.MediasoupSharp.Transport;

namespace Antelcat.MediasoupSharp.DataProducer;

public class DataProducerInternal(string routerId, string transportId, string dataProducerId)
    : TransportInternal(routerId, transportId)
{
    /// <summary>
    /// DataProducer id.
    /// </summary>
    public string DataProducerId { get; } = dataProducerId;
}