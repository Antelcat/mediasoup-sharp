using Antelcat.MediasoupSharp.Transport;

namespace Antelcat.MediasoupSharp.Producer;

public class ProducerInternal(string routerId, string transportId, string producerId)
    : TransportInternal(routerId, transportId)
{
    /// <summary>
    /// Producer id.
    /// </summary>
    public string ProducerId { get; } = producerId;
}