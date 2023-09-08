namespace MediasoupSharp.Producer;

public record ProducerObserverEvents
{
    public List<object> Close { get; set; } = new();
    public List<object> Pause { get; set; } = new();
    public List<object> Resume { get; set; } = new();
    public Tuple<List<ProducerScore>> Score { get; set; }
    public Tuple<ProducerVideoOrientation> Videoorientationchange { get; set; }
    public Tuple<ProducerTraceEventData> Trace { get; set; }
}