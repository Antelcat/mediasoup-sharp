namespace MediasoupSharp.RtpObserver;

public record RtpObserverObserverEvents
{
    public List<object> Close { get; set; } = new();
    public List<object> Pause { get; set; } = new();
    public List<object> Resume { get; set; } = new();
    public Tuple<Producer.Producer> AddProducer { get; set; }
    public Tuple<Producer.Producer> RemoveProducer { get; set; } 
}