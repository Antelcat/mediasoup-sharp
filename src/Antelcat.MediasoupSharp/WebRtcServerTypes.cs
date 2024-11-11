﻿global using WebRtcServerObserver = Antelcat.MediasoupSharp.EnhancedEventEmitter<Antelcat.MediasoupSharp.WebRtcServerObserverEvents>;
using FBS.Transport;

namespace Antelcat.MediasoupSharp;

public record WebRtcServerOptions<TWebRtcServerAppData>
{
    /// <summary>
    /// Listen infos.
    /// </summary>
    public ListenInfoT[] ListenInfos { get; set; } = [];

    /// <summary>
    /// Custom application data.
    /// </summary>
    public TWebRtcServerAppData? AppData { get; set; }
}

public abstract class WebRtcServerEvents
{
    public object? WorkerClose;

    public (string, Exception) ListenerError;

    // Private events.
    public object? close;
}

public abstract class WebRtcServerObserverEvents
{
    public          object?          Close;
    public required IWebRtcTransport WebrtcTransportHandled;
    public required IWebRtcTransport WebrtcTransportUnhandled;
}

public interface IWebRtcServer<TWebRtcServerAppData> : IEnhancedEventEmitter<WebRtcServerEvents>, IWebRtcServer
{
    TWebRtcServerAppData AppData { get; set; }
}