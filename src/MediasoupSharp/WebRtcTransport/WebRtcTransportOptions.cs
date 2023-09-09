﻿using MediasoupSharp.Transport;

namespace MediasoupSharp.WebRtcTransport;

public class WebRtcTransportOptions<TWebRtcTransportAppData> : WebRtcTransportOptionsBase<TWebRtcTransportAppData>
{
    /// <summary>
    /// Instance of WebRtcServer. Mandatory unless listenIps is given.
    /// </summary>
    public WebRtcServer.WebRtcServer? WebRtcServer { get; set; }

    /// <summary>
    /// Listening IP address or addresses in order of preference (first one is the
    /// preferred one).
    /// </summary>
    public TransportListenIp[]? ListenIps { get; set; }

    /// <summary>
    /// Fixed port to listen on instead of selecting automatically from Worker's port
    /// range.
    /// <para>mediasoup-work needs >= 0</para>
    /// </summary>
    public ushort? Port { get; set; } = 0;
}