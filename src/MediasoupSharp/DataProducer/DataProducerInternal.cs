using MediasoupSharp.Transport;

namespace MediasoupSharp.DataProducer;

public record DataProducerInternal : TransportInternal
{
    /// <summary>
    /// DataProducer id.
    /// </summary>
    public string DataProducerId { get; set; }
 
}