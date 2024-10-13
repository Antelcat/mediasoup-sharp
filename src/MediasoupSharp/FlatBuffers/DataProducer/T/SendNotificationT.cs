namespace MediasoupSharp.FlatBuffers.DataProducer.T;

public class SendNotificationT
{
    public uint Ppid { get; set; }

    public byte[] Data { get; set; }

    public List<ushort>? Subchannels { get; set; }

    public ushort? RequiredSubchannel { get; set; }
}
