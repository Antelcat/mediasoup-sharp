namespace MediasoupSharp.DirectTransport;

public class DirectTransportOptions<TDirectTransportAppData>
{
    /// <summary>
    /// Maximum allowed size for direct messages sent from DataProducers.
    /// Default 262144.
    /// </summary>
    public int MaxMessageSize { get; set; } = 262144;

    /// <summary>
    /// Custom application data.
    /// </summary>
    public TDirectTransportAppData? AppData { get; set; }
}