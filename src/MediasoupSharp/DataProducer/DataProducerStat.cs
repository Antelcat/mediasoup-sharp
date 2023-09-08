namespace MediasoupSharp.DataProducer;

public record DataProducerStat
{
    public string Type { get; set; }
    public int Timestamp { get; set; }
    public string Label { get; set; }
    public string Protocol { get; set; }
    public int MessagesReceived { get; set; }
    public int BytesReceived { get; set; }
}