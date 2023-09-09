
namespace MediasoupSharp.WebRtcTransport;

public record DtlsParameters
{
    public DtlsRole? Role { get; set; }
    public List<DtlsFingerprint> Fingerprints { get; set; } = new();
}