using MediasoupSharp.Transport;

namespace MediasoupSharp.Consumer;

public class ConsumerInternal : TransportInternal
{
    /// <summary>
    /// Consumer id.
    /// </summary>
    public string ConsumerId { get; }

    public ConsumerInternal(string routerId, string transportId, string consumerId) : base(routerId, transportId)
    {
        ConsumerId = consumerId;
    }
}
