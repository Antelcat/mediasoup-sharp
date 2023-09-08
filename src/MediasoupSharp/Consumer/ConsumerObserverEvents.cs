namespace MediasoupSharp.Consumer;

public record ConsumerObserverEvents
{
    public List<object> Close { get; set; } = new();
    public List<object> Pause { get; set; } = new();
    public List<object> Resume { get; set; } = new();
    public Tuple<ConsumerScore> Score { get; set; }
    public Tuple<ConsumerLayers?> Layerschange { get; set; }
    public Tuple<ConsumerTraceEventData> Trace { get; set; }
}