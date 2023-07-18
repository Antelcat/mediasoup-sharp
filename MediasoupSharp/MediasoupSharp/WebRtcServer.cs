namespace MediasoupSharp;

public record WebRtcServerOptions<TWebRtcServerAppData>(
    List<WebRtcServerListenInfo> ListenInfos,
    TWebRtcServerAppData? AppData)
    where TWebRtcServerAppData : AppData;

public record WebRtcServerListenInfo(
    TransportProtocol Protocol,
    string Ip,
    string AnnouncedIp,
    Number Port);

public record WebRtcServerEvents(
    List<Action> WorkerClose,
    List<Action> @Close);

public record WebRtcServerObserverEvents(
    List<Action> Close,
    List<WebRtcTransport> Webrtctransporthandled,
    List<WebRtcTransport> Webrtctransportunhandled);
    
internal record WebRtcServerInternal(string WebRtcServerId);

public class WebRtcServer<TWebRtcServerAppData> : EnhancedEventEmitter<WebRtcServerEvents>
    where TWebRtcServerAppData : AppData, new()
{
    internal readonly WebRtcServerInternal @internal;

    internal readonly Channel @channel;

    private bool @closed;

    private TWebRtcServerAppData @appData;

    private readonly Dictionary<string, WebRtcTransport> @webRtcTransports = new();

    private readonly EnhancedEventEmitter<WebRtcServerObserverEvents> @observer = new();

    private WebRtcServer(
        WebRtcServerInternal @internal,
        Channel channel,
        TWebRtcServerAppData? appData)
    {
        this.@internal = @internal;
        this.channel = channel;
        this.appData = appData ?? new();
    }

    public string Id => @internal.WebRtcServerId;

    public bool Closed => this.closed;

    public TWebRtcServerAppData AppData
    {
        get => appData;
        set => appData = value;
    }

    public EnhancedEventEmitter<WebRtcServerObserverEvents> Observer => this.observer;

    public Dictionary<string, WebRtcTransport> WebRtcTransportsForTesting => WebRtcTransportsForTesting;

    public void Close()
    {
        if (closed)
        {
            return;
        }

        closed = true;
        var reqData = new WebRtcServerInternal(this.@internal.WebRtcServerId);
        channel.request("worker.closeWebRtcServer", null, reqData);
            .catch(() => { }); 
        
        // Close every WebRtcTransport.
        foreach (var webRtcTransport in this.webRtcTransports.Values)
        {
            webRtcTransport.listenServerClosed();
            // Emit observer event.
            observer.safeEmit('webrtctransportunhandled', webRtcTransport);
        }

        this.webRtcTransports.Clear();

        this.emit('@close');

// Emit observer event.
        this.observer.safeEmit('close');
    }
}