using Antelcat.MediasoupSharp.EnhancedEvent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Antelcat.MediasoupSharp.Logger;

internal class Logger(ILogger proxy) : EnhancedEventEmitter, ILogger
{
    internal static EnhancedEventEmitter<LoggerEmitterEvents>? DebugLogEmitter { private get; set; }
    internal static EnhancedEventEmitter<LoggerEmitterEvents>? WarnLogEmitter  { private get; set; }
    internal static EnhancedEventEmitter<LoggerEmitterEvents>? ErrorLogEmitter { private get; set; }
    
    
    public Logger(string categoryName) : this(LoggerFactory.CreateLogger(categoryName)) { }
    
    public static ILoggerFactory LoggerFactory { get; set; } = new NullLoggerFactory();
    
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        proxy.Log(logLevel, eventId, state, exception, formatter);
        switch (logLevel)
        {
            case LogLevel.Debug:
                DebugLogEmitter?.Emit(static x => x.debuglog, ($"{eventId}", $"{state}"));
                break;
            case LogLevel.Warning:
                WarnLogEmitter?.Emit(static x => x.warnlog, ($"{eventId}", $"{state}"));
                break;
            case LogLevel.Error:
                ErrorLogEmitter?.Emit(static x => x.errorlog, ($"{eventId}", $"{state}", exception));
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