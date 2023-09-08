namespace MediasoupSharp.Internal;

internal delegate Task EventHandler(params object[]? args);

internal interface IEventEmitter
{
    IDisposable On(string name, Func<object[]?, Task> handler);
    IDisposable AddEventListener(string name, Func<object[]?, Task> handler);

    void Off(string name, Func<object[]?, Task> handler);
    void RemoveListener(string name, Func<object[]?, Task> handler);

    void RemoveAllListeners(string name);
    Task Emit(string name, params object[]? data);
}

internal class EventEmitter
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

    private readonly Dictionary<string, ValueTuple<EventHandler?, ReaderWriterLockSlim>> namedHandlers = new();
    private readonly ReaderWriterLockSlim readerWriterLock = new();

    private ValueTuple<EventHandler?, ReaderWriterLockSlim> CreateHandlers(string name)
    {
        if (namedHandlers.TryGetValue(name, out var handlers)) return handlers;
        readerWriterLock.EnterWriteLock();
        if (namedHandlers.TryGetValue(name, out handlers)) return handlers;
        handlers = new ValueTuple<EventHandler?, ReaderWriterLockSlim>(
            null,
            new ReaderWriterLockSlim());
        namedHandlers.Add(name, handlers);
        readerWriterLock.ExitWriteLock();
        return handlers;
    }

    public IDisposable On(string name, EventHandler handler)
    {
        var tuple = CreateHandlers(name);
        tuple.Item1 += handler;
        return new EventListener(() => tuple.Item1 -= handler);
    }

    public IDisposable AddEventListener(string name, EventHandler handler) => On(name, handler);

    public void Off(string name, EventHandler handler)
    {
        if (!namedHandlers.TryGetValue(name, out var tuple)) return;
        tuple.Item1 -= handler;
    }

    public void RemoveListener(string name, EventHandler handler) => Off(name, handler);

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
        await handlers.Item1!.Invoke(data);
        handlers.Item2.EnterReadLock();
    }

    protected int ListenerCount(string name) => namedHandlers.TryGetValue(name, out var list)
        ? list.Item1!.GetInvocationList().Length
        : 0;
}