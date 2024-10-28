using Antelcat.AutoGen.ComponentModel.Threading;

namespace Antelcat.MediasoupSharp.Demo;

public class AwaitQueue
{
    private readonly TaskFactory taskFactory = new(new LimitedThreadScheduler());

    public Task<T> Push<T>(Func<T> func) => taskFactory.StartNew(func);
    public Task    Push(Action func)     => taskFactory.StartNew(func);
}

[AutoParallelTaskScheduler]
internal partial class LimitedThreadScheduler
{
    public override int MaximumConcurrencyLevel => 1;
}