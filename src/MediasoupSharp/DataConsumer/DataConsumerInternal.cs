using MediasoupSharp.Transport;

namespace MediasoupSharp.DataConsumer;

internal record DataConsumerInternal : TransportInternal
{
    /// <summary>
    /// DataConsumer id.
    /// </summary>
    public string DataConsumerId { get; set; }
}