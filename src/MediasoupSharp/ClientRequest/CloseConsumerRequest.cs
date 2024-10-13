namespace MediasoupSharp.ClientRequest;

public class CloseConsumerRequest
{
    public string? PeerId { get; set; }

    public HashSet<string>? ProducerIds { get; set; }

    public HashSet<string>? ConsumerIds { get; set; }
}
