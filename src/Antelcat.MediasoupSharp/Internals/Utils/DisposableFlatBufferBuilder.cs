using Google.FlatBuffers;

namespace Antelcat.MediasoupSharp.Internals.Utils;

public abstract class DisposableFlatBufferBuilder(int initialSize) : FlatBufferBuilder(initialSize), IDisposable
{
    public abstract void Dispose();
}