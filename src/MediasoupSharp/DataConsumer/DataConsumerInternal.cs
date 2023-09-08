using MediasoupSharp.Transport;

namespace MediasoupSharp.DataConsumer;

public record DataConsumerInternal : TransportInternal
{
    /// <summary>
    /// DataConsumer id.
    /// </summary>
    public string DataConsumerId { get; set; }
}