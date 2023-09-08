namespace MediasoupSharp.Consumer;

public record ConsumerStat
{
    // Common to all RtpStreams.
    public string Type { get; set; }
    public int Timestamp { get; set; }
    public int Ssrc { get; set; }
    public int? RtxSsrc { get; set; }
    public string Kind { get; set; }
    public string MimeType { get; set; }
    public int PacketsLost { get; set; }
    public int FractionLost { get; set; }
    public int PacketsDiscarded { get; set; }
    public int PacketsRetransmitted { get; set; }
    public int PacketsRepaired { get; set; }
    public int NackCount { get; set; }
    public int NackPacketCount { get; set; }
    public int PliCount { get; set; }
    public int FirCount { get; set; }
    public int Score { get; set; }
    public int PacketCount { get; set; }
    public int ByteCount { get; set; }
    public int Bitrate { get; set; }
    public int? RoundTripTime { get; set; }
}