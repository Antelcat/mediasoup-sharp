namespace MediasoupSharp.DataConsumer;

public record DataConsumerObserverEvents
{
    public List<object> Close { get; set; } = new();
}