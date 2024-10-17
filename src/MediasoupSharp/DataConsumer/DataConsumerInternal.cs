using MediasoupSharp.Transport;

namespace MediasoupSharp.DataConsumer;

public class DataConsumerInternal : TransportInternal
{
    /// <summary>
    /// DataConsumer id.
    /// </summary>
    public string DataConsumerId { get; }

    public DataConsumerInternal(string routerId, string transportId, string dataConsumerId) : base(routerId, transportId)
    {
        DataConsumerId = dataConsumerId;
    }
}