namespace MediasoupSharp.PipeTransport;

public record PipeConsumerOptions<TConsumerAppData>
{
    /// <summary>
    /// The id of the Producer to consume.
    /// </summary>
    /// <returns></returns>
    public string ProducerId { get; set; }

    /// <summary>
    /// Custom application data.
    /// </summary>
    /// <returns></returns>
    public TConsumerAppData? AppData { get; set; }
}