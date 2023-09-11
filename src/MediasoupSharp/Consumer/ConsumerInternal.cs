using MediasoupSharp.Transport;

namespace MediasoupSharp.Consumer;

internal record ConsumerInternal : TransportInternal
{
    /// <summary>
    /// Consumer id.
    /// </summary>
    public string ConsumerId { get; set; }
}