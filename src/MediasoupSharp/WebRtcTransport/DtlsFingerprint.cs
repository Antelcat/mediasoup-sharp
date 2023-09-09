namespace MediasoupSharp.WebRtcTransport;

public record DtlsFingerprint
{
    public string Algorithm { get; set; }
    public string Value { get; set; }
}