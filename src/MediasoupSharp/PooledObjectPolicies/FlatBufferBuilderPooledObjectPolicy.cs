using Google.FlatBuffers;
using Microsoft.Extensions.ObjectPool;

namespace MediasoupSharp.PooledObjectPolicies;

public class FlatBufferBuilderPooledObjectPolicy(int initialSize) : IPooledObjectPolicy<FlatBufferBuilder>
{
    public FlatBufferBuilder Create() => new(initialSize);

    public bool Return(FlatBufferBuilder obj) => true;
}