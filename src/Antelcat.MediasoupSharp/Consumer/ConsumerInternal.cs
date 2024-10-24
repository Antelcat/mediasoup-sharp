using Antelcat.MediasoupSharp.Transport;

namespace Antelcat.MediasoupSharp.Consumer;

public class ConsumerInternal(string routerId, string transportId, string consumerId)
    : TransportInternal(routerId, transportId)
{
    /// <summary>
    /// Consumer id.
    /// </summary>
    public string ConsumerId { get; } = consumerId;
}