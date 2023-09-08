using MediasoupSharp.Router;

namespace MediasoupSharp.Transport;

public record TransportInternal : RouterInternal
{
    /// <summary>
    /// TransportId id.
    /// </summary>
    public string TransportId { get; set; }
   
}