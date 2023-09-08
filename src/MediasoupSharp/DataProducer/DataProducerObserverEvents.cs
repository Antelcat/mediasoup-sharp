namespace MediasoupSharp.DataProducer;

public record DataProducerObserverEvents
{
    public List<object> Close { get; set; } = new();
}