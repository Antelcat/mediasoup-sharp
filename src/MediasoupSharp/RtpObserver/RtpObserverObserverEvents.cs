using MediasoupSharp.Producer;

namespace MediasoupSharp.RtpObserver;

public record RtpObserverObserverEvents
{
    public   List<object>             Close          { get; set; } = new();
    public   List<object>             Pause          { get; set; } = new();
    public   List<object>             Resume         { get; set; } = new();
    internal Tuple<IProducer> AddProducer    { get; set; }
    internal Tuple<IProducer> RemoveProducer { get; set; }
}