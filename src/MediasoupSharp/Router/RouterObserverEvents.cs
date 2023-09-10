using MediasoupSharp.RtpObserver;
using MediasoupSharp.Transport;

namespace MediasoupSharp.Router;

public class RouterObserverEvents
{
    public List<object>        Close          { get; set; } = new();
    public Tuple<ITransport>   Newtransport   { get; set; }
    public Tuple<IRtpObserver> Newrtpobserver { get; set; }
}