namespace MediasoupSharp.Producer;

public record ProducerEvents
{
    public List<object> Transportclose { get; set; } = new();
    public Tuple<List<ProducerScore>> Score { get; set; }
    public Tuple<ProducerVideoOrientation> Videoorientationchange { get; set; }
    public Tuple<ProducerTraceEventData> Trace { get; set; }
    // Private events.
    private List<object> close { get; set; }
}