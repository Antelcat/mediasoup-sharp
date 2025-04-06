global using DirectTransportObserver = Antelcat.MediasoupSharp.EnhancedEventEmitter<Antelcat.MediasoupSharp.DirectTransportObserverEvents>;

namespace Antelcat.MediasoupSharp;

public class DirectTransportOptions<TDirectTransportAppData>
{
    /// <summary>
    /// Maximum allowed size for direct messages sent from DataProducers.
    /// Default 262144.
    /// </summary>
    public uint MaxMessageSize { get; set; } = 262144;

    /// <summary>
    /// Custom application data.
    /// </summary>
    public TDirectTransportAppData? AppData { get; set; }
}

public abstract class DirectTransportEvents : TransportEvents
{
    public abstract List<byte> Rtcp { get; }
}

public abstract class DirectTransportObserverEvents : TransportObserverEvents
{
    public abstract List<byte> Rtcp { get; }
}

public interface IDirectTransport<TDirectTransportAppData>
    : ITransport<
        TDirectTransportAppData, 
        DirectTransportEvents, 
        DirectTransportObserver
    >, IDirectTransport;