using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace MediasoupSharp.Test;

public class DebugLogger : ILogger
{
    private readonly string? name;

    public DebugLogger(string? name = null)
    {
        this.name = name;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        var stack  = new StackTrace(1, true);
        var frames = stack.GetFrames();
        Debugger.Break();
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        throw new NotImplementedException();
    }
}