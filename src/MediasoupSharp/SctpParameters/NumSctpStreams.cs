namespace MediasoupSharp.SctpParameters;

public record NumSctpStreams
{
    /// <summary>
    /// Initially requested number of outgoing SCTP streams.
    /// </summary>
    public int OS { get; set; }
    
    /// <summary>
    /// Maximum number of incoming SCTP streams.
    /// </summary>
    public int MIS { get; set; }
}