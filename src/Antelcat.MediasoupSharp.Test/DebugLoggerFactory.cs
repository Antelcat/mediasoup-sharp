using Microsoft.Extensions.Logging;

namespace Antelcat.MediasoupSharp.Test;

public class DebugLoggerFactory : ILoggerFactory
{
    public void Dispose()
    {
        
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new DebugLogger(categoryName);
    }

    public void AddProvider(ILoggerProvider provider)
    {
    }
}