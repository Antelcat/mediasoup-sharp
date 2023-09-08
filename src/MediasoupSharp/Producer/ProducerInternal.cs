using MediasoupSharp.Transport;

namespace MediasoupSharp.Producer;

public record ProducerInternal : TransportInternal
{
    public string ProducerId { get; set; }
}