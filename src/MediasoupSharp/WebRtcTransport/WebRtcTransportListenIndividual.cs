using MediasoupSharp.Transport;

namespace MediasoupSharp.WebRtcTransport;

public interface IWebRtcTransportListenIndividual
{
    /// <summary>
    /// Listening IP address or addresses in order of preference (first one is the
    /// preferred one).
    /// <see cref="string"/> or <see cref="TransportListenIp"/>
    /// </summary>
    public List<object> ListenIps { get; set; }

    /// <summary>
    /// Fixed port to listen on instead of selecting automatically from Worker's port
    /// range.
    /// </summary>
    public ushort? Port { get; set; }  // mediasoup-work needs >= 0
}

public class WebRtcTransportListenIndividual : IWebRtcTransportListenIndividual
{
    /// <summary>
    /// Listening IP address or addresses in order of preference (first one is the
    /// preferred one).
    /// <see cref="string"/> or <see cref="TransportListenIp"/>
    /// </summary>
    public List<object> ListenIps { get; set; } = new();

    /// <summary>
    /// Fixed port to listen on instead of selecting automatically from Worker's port
    /// range.
    /// </summary>
    public ushort? Port { get; set; } = 0; // mediasoup-work needs >= 0
}