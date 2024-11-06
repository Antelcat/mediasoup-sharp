using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Antelcat.MediasoupSharp.Test;

public class TypeTests
{
    [Test]
    public void Test()
    {
        var ii = Marshal.SizeOf<II>();
        var a  = Marshal.SizeOf<C>();
        var b  = Marshal.SizeOf<B>();
        Debugger.Break();
    }

    interface II
    {
        public int A { get; set; }
    }

    class C
    {
        public int A;
    }

    abstract class B
    {
        public int A;
    }
}