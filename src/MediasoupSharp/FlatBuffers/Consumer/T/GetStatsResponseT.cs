using FlatBuffers.RtpStream;

namespace MediasoupSharp.FlatBuffers.Consumer.T;

public class GetStatsResponseT
{
    public List<StatsT> Stats { get; set; }
}
