using Google.FlatBuffers;
using Microsoft.Extensions.ObjectPool;

namespace MediasoupSharp.PooledObjectPolicies;

public class FlatBufferBuilderPooledObjectPolicy : IPooledObjectPolicy<FlatBufferBuilder>
{
    private readonly int initialSize;

    public FlatBufferBuilderPooledObjectPolicy(int initialSize)
    {
        this.initialSize = initialSize;
    }

    public FlatBufferBuilder Create()
    {
        return new FlatBufferBuilder(initialSize);
    }

    public bool Return(FlatBufferBuilder obj)
    {
        return true;
    }
}