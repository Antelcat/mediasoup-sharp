using System.Collections.Concurrent;

namespace MediasoupSharp.Channel;

public class OutgoingMessageBuffer<T>
{
	public ConcurrentQueue<T> Queue { get; } = new();

	public IntPtr Handle { get; set; }
}