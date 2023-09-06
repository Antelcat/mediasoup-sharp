namespace MediasoupSharp.RtpObserver;

public class RtpObserverEvents
{
    public List<object> RouterClose { get; set; } = new();
    private List<object> close = new();
}