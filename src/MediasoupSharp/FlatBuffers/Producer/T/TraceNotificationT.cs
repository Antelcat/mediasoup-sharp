namespace MediasoupSharp.FlatBuffers.Producer.T;

public class TraceNotificationT
{
    public global::FlatBuffers.Producer.TraceEventType Type { get; set; }

    public ulong Timestamp { get; set; }

    public global::FlatBuffers.Common.TraceDirection Direction { get; set; }

    public global::FlatBuffers.Producer.TraceInfoUnion Info { get; set; }
}
