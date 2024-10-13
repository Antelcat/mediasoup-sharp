using Google.FlatBuffers;
using Microsoft.Extensions.ObjectPool;

namespace MediasoupSharp.PooledObjectPolicies;

public class FlatBufferBuilderPooledObjectPolicy(int initialSize) : IPooledObjectPolicy<FlatBufferBuilder>
{
    public FlatBufferBuilder Create()
    {
        return new FlatBufferBuilder(initialSize);
    }

    public bool Return(FlatBufferBuilder obj)
    {
        return true;
    }
}