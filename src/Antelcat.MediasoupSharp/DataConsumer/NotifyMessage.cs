namespace Antelcat.MediasoupSharp.DataConsumer;

public class NotifyMessage
{
    public ArraySegment<byte> Message { get; set; }

    public int Ppid { get; set; }
}