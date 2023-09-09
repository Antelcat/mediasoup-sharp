namespace MediasoupSharp.DirectTransport;

public record DirectTransportStat
{
    // Common to all Transports.
    public string Type                     { get; set; }
    public string TransportId              { get; set; }
    public long   Timestamp                { get; set; }
    public long   BytesReceived            { get; set; }
    public int    RecvBitrate              { get; set; }
    public long   BytesSent                { get; set; }
    public int    SendBitrate              { get; set; }
    public long   RtpBytesReceived         { get; set; }
    public int    RtpRecvBitrate           { get; set; }
    public long   RtpBytesSent             { get; set; }
    public int    RtpSendBitrate           { get; set; }
    public int    RtxBytesReceived         { get; set; }
    public int    RtxRecvBitrate           { get; set; }
    public long   RtxBytesSent             { get; set; }
    public int    RtxSendBitrate           { get; set; }
    public long   ProbationBytesSent       { get; set; }
    public int    ProbationSendBitrate     { get; set; }
    public int?   AvailableOutgoingBitrate { get; set; }
    public int?   AvailableIncomingBitrate { get; set; }
    public int?   MaxIncomingBitrate       { get; set; }
}