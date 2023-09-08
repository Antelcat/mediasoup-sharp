namespace MediasoupSharp.Transport;

public record TransportTraceEventData
{
    /// <summary>
    /// Trace type.
    /// </summary>
    public TransportTraceEventType Type { get; set; }

    /// <summary>
    /// Event timestamp.
    /// </summary>
    public long Timestamp { get; set; }

    /// <summary>
    /// Event direction.
    /// in out
    /// </summary>
    public string Direction { get; set; }

    /// <summary>
    /// Per type information.
    /// </summary>
    public object Info { get; set; }
}