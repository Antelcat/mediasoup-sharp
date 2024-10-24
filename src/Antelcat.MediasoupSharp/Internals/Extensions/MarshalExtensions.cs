namespace Antelcat.MediasoupSharp.Internals.Extensions;

internal static class MarshalExtensions
{

    public static byte[] ToBytes(this IntPtr input)
    {
        return IntPtr.Size == sizeof(int)
            ? BitConverter.GetBytes((int)input)
            : BitConverter.GetBytes(input);
    }

    public static IntPtr ToIntPtr(this byte[] input) =>
        (IntPtr)(IntPtr.Size == sizeof(int)
            ? BitConverter.ToInt32(input, 0)
            : BitConverter.ToInt64(input, 0));
}