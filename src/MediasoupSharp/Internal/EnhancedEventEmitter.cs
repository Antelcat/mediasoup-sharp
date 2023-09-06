using Microsoft.Extensions.Logging;

namespace MediasoupSharp.Internal;

public interface IEnhancedEventEmitter
{
    Task<bool> SafeEmit(string name, params object[]? args);
}

public interface IEnhancedEventEmitter<out TEvent> : IEnhancedEventEmitter { }

public class EnhancedEventEmitter : EventEmitter , IEnhancedEventEmitter
{
    private readonly ILogger logger;

    public EnhancedEventEmitter(ILogger logger)
    {
        this.logger = logger;
    }
    
    public async Task<bool> SafeEmit(string name,params object[]? args)
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

public class EnhancedEventEmitter<TEvent> : EnhancedEventEmitter, IEnhancedEventEmitter<TEvent>
{
    public EnhancedEventEmitter(ILogger logger) : base(logger) { }
}