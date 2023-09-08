using Microsoft.Extensions.Logging;

namespace MediasoupSharp.Internal;

internal interface IEnhancedEventEmitter
{
    Task<bool> SafeEmit(string name, params object[]? args);
}

internal interface IEnhancedEventEmitter<out TEvent> : IEnhancedEventEmitter
{
}

internal class EnhancedEventEmitter : EventEmitter, IEnhancedEventEmitter
{
    private readonly ILogger logger;

    internal EnhancedEventEmitter(ILogger logger)
    {
        this.logger = logger;
    }

    public async Task<bool> SafeEmit(string name, params object[]? args)
    {
        var numListeners = ListenerCount(name);
        try
        {
            await Emit(name, args);
            return true;
        }
        catch (Exception e)
        {
            logger.LogError("safeEmit() | event listener threw an error [{Name}]:{S}", name, e.ToString());
            return numListeners > 0;
        }
    }
}

internal class EnhancedEventEmitter<TEvent> : EnhancedEventEmitter, IEnhancedEventEmitter<TEvent>
{
    internal EnhancedEventEmitter(ILogger logger) : base(logger)
    {
    }
}