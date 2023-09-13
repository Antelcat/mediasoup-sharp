using System.Diagnostics;

namespace MediasoupSharp.Errors;

public class InvalidStateError : Exception
{
    public readonly string       Name = "InvalidStateError";
    public readonly StackFrame[] Stack;

    public InvalidStateError(string message) : base(message)
    {
        Stack = new StackTrace(true).GetFrames();
    }
}