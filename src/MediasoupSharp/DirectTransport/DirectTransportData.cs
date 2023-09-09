namespace MediasoupSharp.DirectTransport;

public interface IDirectTransportData
{
    SctpParameters.SctpParameters? SctpParameters { get; set; }
}

public record DirectTransportData : IDirectTransportData
{
    public SctpParameters.SctpParameters? SctpParameters { get; set; }
}