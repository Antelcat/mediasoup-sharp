namespace MediasoupSharp.RtpObserver;

public class RtpObserverObserverEvents
{
    public List<object> Close { get; set; } = new();
    public List<object> Pause { get; set; } = new();
    public List<object> Resume { get; set; } = new();
    public List<Producer.Producer> AddProducer { get; set; } = new();
    public List<Producer.Producer> RemoveProducer { get; set; } = new();
}