namespace MediasoupSharp.Consumer;

public record ConsumerEvents
{
    public List<object> Transportclose { get; set; } = new();
    public List<object> Producerclose { get; set; } = new();
    public List<object> Producerpause { get; set; } = new();
    public List<object> Producerresume { get; set; } = new();
    public Tuple<ConsumerScore> Score { get; set; }
    public Tuple<ConsumerLayers?> Layerschange { get; set; }
    public Tuple<ConsumerTraceEventData> Trace { get; set; }
    public Tuple<byte[]> Rtp { get; set; }
    
    private List<object> close = new();
    private List<object> producerclose = new();

}