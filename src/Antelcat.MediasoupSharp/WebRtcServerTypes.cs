global using WebRtcServerObserver = Antelcat.MediasoupSharp.EnhancedEventEmitter<Antelcat.MediasoupSharp.WebRtcServerObserverEvents>;
using System.Diagnostics.CodeAnalysis;
using Antelcat.MediasoupSharp.FBS.Transport;
using Antelcat.MediasoupSharp.Internals.Extensions;

namespace Antelcat.MediasoupSharp;

[DynamicallyAccessedMembers(ObjectExtensions.CloneMemberTypes)]
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

public abstract class WebRtcServerEvents : BuiltInEvents
{
    public abstract object? WorkerClose { get; }

    // Private events.
    internal abstract object? close { get; }
}

public abstract class WebRtcServerObserverEvents
{
    public abstract object?          Close                    { get; }
    public abstract IWebRtcTransport WebrtcTransportHandled   { get; }
    public abstract IWebRtcTransport WebrtcTransportUnhandled { get; }
}

public interface IWebRtcServer<TWebRtcServerAppData> : IEnhancedEventEmitter<WebRtcServerEvents>, IWebRtcServer
{
    TWebRtcServerAppData AppData { get; set; }
}