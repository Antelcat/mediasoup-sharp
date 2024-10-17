using MediasoupSharp.Router;

namespace MediasoupSharp.Transport;

public class TransportInternal : RouterInternal
{
    /// <summary>
    /// Trannsport id.
    /// </summary>
    public string TransportId { get; }

    public TransportInternal(string routerId, string transportId) : base(routerId)
    {
        TransportId = transportId;
    }
}