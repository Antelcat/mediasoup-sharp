namespace MediasoupSharp.Internal;

public interface IEventEmitter
{
    IDisposable On(string name, Func<object[]?, Task> handler);
    IDisposable AddEventListener(string name, Func<object[]?, Task> handler);

    void Off(string name, Func<object[]?, Task> handler);
    void RemoveListener(string name, Func<object[]?, Task> handler);

    void RemoveAllListeners(string name);
    Task Emit(string name, params object[]? data);

}

public class EventEmitter
{
    private class EventListener : IDisposable
    {
        public EventListener(Action disposeAction)
        {
            this.disposeAction = disposeAction;
        }

        private Action? disposeAction;

        public void Dispose()
        {
            if (disposeAction == null) return;
            disposeAction.Invoke();
            disposeAction = null;
        }
    }

    private readonly Dictionary<string, Tuple<List<Func<object[]?, Task>>, ReaderWriterLockSlim>> namedHandlers = new();
    private readonly ReaderWriterLockSlim readerWriterLock = new();

    private Tuple<List<Func<object[]?, Task>>, ReaderWriterLockSlim> CreateHandlers(string name)
    {
        if (namedHandlers.TryGetValue(name, out var handlers)) return handlers;
        readerWriterLock.EnterWriteLock();
        if (namedHandlers.TryGetValue(name, out handlers)) return handlers;
        handlers = new Tuple<List<Func<object[]?, Task>>, ReaderWriterLockSlim>(
            new List<Func<object[]?, Task>>(),
            new ReaderWriterLockSlim());
        namedHandlers.Add(name, handlers);
        readerWriterLock.ExitWriteLock();
        return handlers;
    }

    public IDisposable On(string name, Func<object[]?, Task> handler)
    {
        var tuple = CreateHandlers(name);
        tuple.Item1.Add(handler);
        return new EventListener(() => tuple.Item1.Remove(handler));
    }

    public IDisposable AddEventListener(string name, Func<object[]?, Task> handler) => On(name, handler);

    public void Off(string name, Func<object[]?, Task> handler)
    {
        if (!namedHandlers.TryGetValue(name, out var tuple)) return;
        tuple.Item1.Remove(handler);
    }

    public void RemoveListener(string name, Func<object[]?, Task> handler) => Off(name, handler);

    public void RemoveAllListeners(string name)
    {
        if (!namedHandlers.TryGetValue(name, out _)) return;
        readerWriterLock.EnterWriteLock();
        namedHandlers.Remove(name);
        readerWriterLock.ExitWriteLock();
    }

    public async Task Emit(string name, params object[]? data)
    {
        if (!namedHandlers.TryGetValue(name, out var handlers)) return;
        handlers.Item2.EnterReadLock();
        foreach (var handler in handlers.Item1)
        {
            await handler(data);
        }

        handlers.Item2.EnterReadLock();
    }

    protected int ListenerCount(string name) => namedHandlers.TryGetValue(name, out var list)
        ? list.Item1.Count
        : 0;
}