namespace MediasoupSharp.Producer;

public record ProducerTraceEventData
{
    /// <summary>
    /// Trace type.
    /// </summary>
    public ProducerTraceEventType Type { get; set; }

    /// <summary>
    /// Event timestamp.
    /// </summary>
    public long Timestamp { get; set; }
   
    /// <summary>
    /// Event direction.
    /// <example>in</example>
    /// <example>out</example>
    /// </summary>
    /// <returns></returns>
    public string Direction { get; set; }

    /// <summary>
    /// Per type information.
    /// </summary>
    public object Info { get; set; }
}