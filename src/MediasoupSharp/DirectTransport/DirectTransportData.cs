namespace MediasoupSharp.DirectTransport;

public record DirectTransportData
{
    public SctpParameters.SctpParameters? SctpParameters { get; set; }
}