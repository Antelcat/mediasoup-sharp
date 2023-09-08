namespace MediasoupSharp.WebRtcTransport;

public record IceParameters
{
    public string UsernameFragment { get; set; }
    public string Password { get; set; }
    public bool? IceLite { get; set; }
}