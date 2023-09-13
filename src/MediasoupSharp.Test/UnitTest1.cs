using MediasoupSharp.Internal;
using MediasoupSharp.Worker;

namespace MediasoupSharp.Test;

public class Tests
{
    [SetUp]
    public void Setup()
    {
        var aas = new List<int>(){ 1,2,3,4,5 }.DeepClone();
    }

    [Test]
    public void Test1()
    {
        Assert.Pass();
    }
}