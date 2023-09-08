﻿namespace MediasoupSharp.Consumer;

public record ConsumerTraceEventData
{
    /// <summary>
    /// Trace type.
    /// </summary>
    public ConsumerTraceEventType Type { get; set; }

    /// <summary>
    /// Event timestamp.
    /// </summary>
    public long Timestamp;

    /// <summary>
    /// Event direction.
    /// <example>in</example>
    /// <example>out</example>
    /// </summary>
    public string Direction { get; set; } = "in";

    /// <summary>
    /// Per type information.
    /// </summary>
    public object Info { get; set; }
}