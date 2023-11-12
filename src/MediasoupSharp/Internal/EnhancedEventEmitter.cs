using Microsoft.Extensions.Logging;

namespace MediasoupSharp.Internal;

public interface IEnhancedEventEmitter
{
    Task<bool> SafeEmit(string name, params object[]? args);
}

public interface IEnhancedEventEmitter<TEvent> : IEnhancedEventEmitter { }

internal class EnhancedEventEmitter : EventEmitter, IEnhancedEventEmitter
{
    protected EnhancedEventEmitter(ILoggerFactory? loggerFactory = null)
    {
        logger = loggerFactory?.CreateLogger(GetType());
    }

    private readonly ILogger? logger;

    public async Task<bool> SafeEmit(string name, params object?[]? args)
    {
        var numListeners = ListenerCount(name);
        try
        {
            await Emit(name, args);
            return true;
        }
        catch (Exception e)
        {
            logger?.LogError("safeEmit() | event listener threw an error [{Name}]:{S}", name, e.ToString());
            return numListeners > 0;
        }
    }
}

internal class EnhancedEventEmitter<TEvent> : EnhancedEventEmitter, IEnhancedEventEmitter<TEvent>
{
    public EnhancedEventEmitter(ILoggerFactory? loggerFactory = null) : base(loggerFactory)
    {
    }
}