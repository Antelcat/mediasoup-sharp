using MediasoupSharp.Transport;

namespace MediasoupSharp.DataProducer;

internal record DataProducerInternal : TransportInternal
{
    /// <summary>
    /// DataProducer id.
    /// </summary>
    public string DataProducerId { get; set; }
 
}