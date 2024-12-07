using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Antelcat.MediasoupSharp;

internal class Logger(ILogger proxy) : EnhancedEventEmitter, ILogger
{
    internal static EnhancedEventEmitter<LoggerEmitterEvents>? DebugLogEmitter { private get; set; }
    internal static EnhancedEventEmitter<LoggerEmitterEvents>? WarnLogEmitter  { private get; set; }
    internal static EnhancedEventEmitter<LoggerEmitterEvents>? ErrorLogEmitter { private get; set; }
    
    
    public Logger(string categoryName) : this(LoggerFactory.CreateLogger(categoryName)) { }
    
    public static ILoggerFactory LoggerFactory { get; set; } = new NullLoggerFactory();
    
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        proxy.Log(logLevel, eventId, state, exception, (s, e) => "mediasoup:" + formatter(s, e));
        switch (logLevel)
        {
            case LogLevel.Debug:
                DebugLogEmitter?.SafeEmit(static x => x.DebugLog, ($"{eventId}", $"{state}"));
                break;
            case LogLevel.Warning:
                WarnLogEmitter?.SafeEmit(static x => x.WarnLog, ($"{eventId}", $"{state}"));
                break;
            case LogLevel.Error:
                ErrorLogEmitter?.SafeEmit(static x => x.ErrorLog, ($"{eventId}", $"{state}", exception));
                break;
        }
    }

    public bool IsEnabled(LogLevel logLevel) => proxy.IsEnabled(logLevel);

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => proxy.BeginScope(state);
}

internal class Logger<TCategoryName>(ILogger<TCategoryName> proxy) : Logger(proxy), ILogger<TCategoryName>
{
    public Logger() : this(LoggerFactory.CreateLogger<TCategoryName>())
    {
    }
}

public abstract class LoggerEmitterEvents
{
    public (string, string)            DebugLog;
    public (string, string)            WarnLog;
    public (string, string, Exception) ErrorLog;
}