namespace LightweightUv;

public partial class UvStream
{
    public bool Writeable { get; set; }
    
    public bool Readable { get; set; }
    
    public event Action<ArraySegment<byte>>? Data;

    public event Action<Exception>? Error;

    public event Action? Complete;

    public event Action? Closed;

    public event Action? Drain;
}