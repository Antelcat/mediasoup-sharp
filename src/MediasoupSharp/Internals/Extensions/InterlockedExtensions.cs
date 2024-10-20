namespace MediasoupSharp.Internals.Extensions;

internal static class InterlockedExtensions
{
    /// <summary>
    /// unsigned equivalent of <see cref="Interlocked.Increment(ref int)"/>
    /// </summary>
    public static uint Increment(this ref uint location)
    {
        var incrementedSigned = Interlocked.Increment(ref System.Runtime.CompilerServices.Unsafe.As<uint, int>(ref location));
        return System.Runtime.CompilerServices.Unsafe.As<int, uint>(ref incrementedSigned);
    }

    /// <summary>
    /// unsigned equivalent of <see cref="Interlocked.Increment(ref long)"/>
    /// </summary>
    public static ulong Increment(this ref ulong location)
    {
        var incrementedSigned = Interlocked.Increment(ref System.Runtime.CompilerServices.Unsafe.As<ulong, long>(ref location));
        return System.Runtime.CompilerServices.Unsafe.As<long, ulong>(ref incrementedSigned);
    }
}