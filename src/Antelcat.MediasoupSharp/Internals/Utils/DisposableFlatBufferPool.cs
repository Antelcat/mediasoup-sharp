using Microsoft.Extensions.ObjectPool;

namespace Antelcat.MediasoupSharp.Internals.Utils;

public class DisposableFlatBufferPool : ObjectPool<DisposableFlatBufferBuilder>
{
    public DisposableFlatBufferPool(int size = 1024)
    {
        var policy = new DisposableFlatBufferBuilderPooledObjectPolicy(size);
        pool        = new DefaultObjectPoolProvider().Create(policy);
        policy.Pool = pool;
    }

    private readonly ObjectPool<DisposableFlatBufferBuilder> pool;

    public DisposableFlatBufferBuilder Get(int size) => new RealDisposableFlatBufferBuilder(size, pool);

    public override DisposableFlatBufferBuilder Get() => pool.Get();

    public override void Return(DisposableFlatBufferBuilder obj) => pool.Return(obj);

    private class DisposableFlatBufferBuilderPooledObjectPolicy(int initialSize)
        : IPooledObjectPolicy<DisposableFlatBufferBuilder>
    {
        public ObjectPool<DisposableFlatBufferBuilder> Pool { get; set; } = null!;

        public DisposableFlatBufferBuilder Create() => new RealDisposableFlatBufferBuilder(initialSize, Pool);

        public bool Return(DisposableFlatBufferBuilder obj) =>
            obj is RealDisposableFlatBufferBuilder { Disposed: false };
    }

    private class RealDisposableFlatBufferBuilder(int size, ObjectPool<DisposableFlatBufferBuilder> pool)
        : DisposableFlatBufferBuilder(size)
    {
        public bool Disposed { get; private set; }

        public override void Dispose()
        {
            if (Disposed) return;
            Disposed = true;
            pool.Return(this);
        }
    }
}