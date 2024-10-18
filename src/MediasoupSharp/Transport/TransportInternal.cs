using MediasoupSharp.Router;

namespace MediasoupSharp.Transport;

public class TransportInternal(string routerId, string transportId) : RouterInternal(routerId)
{
    /// <summary>
    /// Trannsport id.
    /// </summary>
    public string TransportId { get; } = transportId;
}