namespace MediasoupSharp.RtpObserver;

public record RtpObserverObserverEvents
{
    public   List<object>             Close          { get; set; } = new();
    public   List<object>             Pause          { get; set; } = new();
    public   List<object>             Resume         { get; set; } = new();
    internal Tuple<Producer.Producer> AddProducer    { get; set; }
    internal Tuple<Producer.Producer> RemoveProducer { get; set; }
}