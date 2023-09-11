using MediasoupSharp.Transport;
using MediasoupSharp.WebRtcServer;

namespace MediasoupSharp.WebRtcTransport;

public class WebRtcTransportOptions<TWebRtcTransportAppData> : WebRtcTransportOptionsBase<TWebRtcTransportAppData>
{
    /// <summary>
    /// Instance of WebRtcServer. Mandatory unless listenIps is given.
    /// </summary>
    public IWebRtcServer? WebRtcServer { get; set; }

    /// <summary>
    /// Listening IP address or addresses in order of preference (first one is the
    /// preferred one).
    ///  (<see cref="TransportListenIp"/> | <see cref="string"/>)
    /// </summary>
    public List<object>? ListenIps { get; set; }

    /// <summary>
    /// Fixed port to listen on instead of selecting automatically from Worker's port
    /// range.
    /// <para>mediasoup-work needs >= 0</para>
    /// </summary>
    public ushort? Port { get; set; } = 0;
}