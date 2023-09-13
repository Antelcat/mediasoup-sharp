using System.Diagnostics;

namespace MediasoupSharp.Errors;

public class UnsupportedError : Exception
{
    public readonly string     Name = "UnsupportedError";
    public readonly StackFrame[] Stack;

    public UnsupportedError(string message) : base(message)
    {
        Stack = new StackTrace(true).GetFrames();
    }
}