using MediasoupSharp.Router;

namespace MediasoupSharp.Worker;

public record WorkerObserverEvents
{
    public List<object> Close { get; set; } = new();
    
    internal Tuple<WebRtcServer.WebRtcServer> Newwebrtcserver { get; set; } 
    
    internal Tuple<IRouter> Newrouter { get; set; }
}