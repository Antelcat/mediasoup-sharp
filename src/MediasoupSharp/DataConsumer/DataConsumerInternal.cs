using MediasoupSharp.Transport;

namespace MediasoupSharp.DataConsumer;

public class DataConsumerInternal(string routerId, string transportId, string dataConsumerId)
    : TransportInternal(routerId, transportId)
{
    /// <summary>
    /// DataConsumer id.
    /// </summary>
    public string DataConsumerId { get; } = dataConsumerId;
}