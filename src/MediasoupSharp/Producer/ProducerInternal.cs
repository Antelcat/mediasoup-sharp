using MediasoupSharp.Transport;

namespace MediasoupSharp.Producer;

internal record ProducerInternal : TransportInternal
{
    public string ProducerId { get; set; }
}