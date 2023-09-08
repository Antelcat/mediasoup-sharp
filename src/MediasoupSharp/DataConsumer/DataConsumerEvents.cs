namespace MediasoupSharp.DataConsumer;

public record DataConsumerEvents
{
    public List<object> Transportclose { get; set; } = new();
    public List<object> Dataproducerclose { get; set; } = new();
    private Tuple<byte[], int> Message { get; set; }
    public List<object> Sctpsendbufferfull { get; set; } = new();

    public Tuple<int> Bufferedamountlow { get; set; }

    // Private events.
    public List<object> @close { get; set; } = new();
    public List<object> @dataproducerclose { get; set; } = new();
}