using System.Diagnostics;

namespace MediasoupSharp;

public class UnsupportedError : Exception
{
    public readonly string name;
    private readonly string? stack;
    public UnsupportedError(string message) : base(message)
    {
        name = nameof(UnsupportedError);
        stack = new Exception().StackTrace;
    }    
}

public class InvalidStateError : Exception
{
    public readonly string name;
    private readonly string? stack;
    public InvalidStateError(string message) : base(message)
    {
        name = nameof(InvalidStateError);
        stack = new Exception().StackTrace;
    }    
}