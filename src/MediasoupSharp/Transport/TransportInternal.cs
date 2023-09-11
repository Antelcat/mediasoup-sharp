using MediasoupSharp.Router;

namespace MediasoupSharp.Transport;

internal record TransportInternal : RouterInternal
{
    /// <summary>
    /// TransportId id.
    /// </summary>
    public string TransportId { get; set; }
}