using MediasoupSharp.Worker;
using Microsoft.Extensions.Logging;

namespace MediasoupSharp.Test;

public class Tests
{
    [SetUp]
    public void Setup()
    {
        new Worker<object>(new WorkerSettings<object>()
        {
            LogLevel = WorkerLogLevel.debug
        });
    }

    [Test]
    public void Test1()
    {
        Assert.Pass();
    }
}