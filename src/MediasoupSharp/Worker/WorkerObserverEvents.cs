namespace MediasoupSharp.Worker;

public record WorkerObserverEvents
{
    public List<object> Close { get; set; } = new();
    
    internal Tuple<WebRtcServer.WebRtcServer> Newwebrtcserver { get; set; } 
    
    internal Tuple<Router.Router> Newrouter { get; set; }
}