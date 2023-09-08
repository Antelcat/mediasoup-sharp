namespace MediasoupSharp.DataProducer;

public record DataProducerEvents
{
    public List<object> Transportclose { get; set; } = new();
    private List<object> close = new();
}