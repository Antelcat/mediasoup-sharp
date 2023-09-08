namespace MediasoupSharp.Transport;

public record TransportTuple
{
    public string LocalIp { get; set; }
    public int LocalPort { get; set; }
    public string? RemoteIp { get; set; }
    public int? RemotePort { get; set; }
    public TransportProtocol Protocol { get; set; }
}