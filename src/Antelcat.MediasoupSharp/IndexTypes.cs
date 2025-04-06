using Antelcat.MediasoupSharp;

namespace Antelcat.MediasoupSharp;
using Observer = IEnhancedEventEmitter<ObserverEvents>;

public abstract class ObserverEvents
{
    public required IWorker NewWorker;
}

public class LogEventListeners
{
    public Action<string, string>?            OnDebug;
    public Action<string, string>?            OnWarn;
    public Action<string, string, Exception>? OnError;
}