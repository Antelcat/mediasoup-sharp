namespace MediasoupSharp.DataConsumer;

public record DataConsumerStat
{
    public string Type { get; set; }
    public int Timestamp { get; set; }
    public string Label { get; set; }
    public string Protocol { get; set; }
    public int MessagesSent { get; set; }
    public int BytesSent { get; set; }
    public int BufferedAmount { get; set; }
}