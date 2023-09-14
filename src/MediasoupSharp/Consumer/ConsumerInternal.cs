using MediasoupSharp.Transport;

namespace MediasoupSharp.Consumer;

public record ConsumerInternal : TransportInternal
{
    /// <summary>
    /// Consumer id.
    /// </summary>
    public string ConsumerId { get; set; }
}