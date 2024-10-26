namespace Antelcat.MediasoupSharp.EnhancedEvent;

public class EnhancedEventEmitter : NodeSharp.Events.EventEmitter, IEventEmitter
{
    public void On(string eventName, Func<string, object?, Task> method) =>
        base.On(eventName, (object[] args) => method(eventName, args[0]));

    public void Emit(string eventName, object? data = null) => base.Emit(eventName, data);

    public void RemoveListener(string eventName, Func<string, object?, Task> method) => throw new NotSupportedException();

    public new void RemoveAllListeners(string eventName) => base.RemoveAllListeners(eventName);
}

#if False
public class EventEmitter : IEventEmitter
{
    /*
    {
        "subscribe_event",
        [
            HandleSubscribe<List<object>>,
            DoDbWork<List<object>>,
            SendInfo<List<object>>
        ],
         "listen_event",
        [
            HandleListen<List<object>>
        ]
    }
    */

    private const char EventSeparator = ',';

    private readonly Dictionary<string, List<Func<string, object?, Task>>> events;

    private readonly ReaderWriterLockSlim readerWriterLock;

    /// <summary>
    /// The EventEmitter object to subscribe to events with
    /// </summary>
    public EventEmitter()
    {
        events           = new Dictionary<string, List<Func<string, object?, Task>>>();
        readerWriterLock = new ReaderWriterLockSlim();
    }

    /// <summary>
    /// Whenever eventName is emitted, the methods attached to this event will be called
    /// </summary>
    /// <param name="eventNames">Event name to subscribe to</param>
    /// <param name="method">Method to add to the event</param>
    public void On(string eventNames, Func<string, object?, Task> method)
    {
        readerWriterLock.EnterWriteLock();
        var eventNameList = eventNames.Split(EventSeparator, StringSplitOptions.RemoveEmptyEntries);
        foreach (var eventName in eventNameList)
        {
            if (events.TryGetValue(eventName, out var subscribedMethods))
            {
                subscribedMethods.Add(method);
            }
            else
            {
                events.Add(eventName, [method]);
            }
        }

        readerWriterLock.ExitWriteLock();
    }

    /// <summary>
    /// Emits the event and runs all associated methods asynchronously
    /// </summary>
    /// <param name="eventName">The event name to call methods for</param>
    /// <param name="data">The data to call all the methods with</param>
    public void Emit(string eventName, object? data = null)
    {
        readerWriterLock.EnterReadLock();
        if (!events.TryGetValue(eventName, out var subscribedMethods))
        {
            //throw new DoesNotExistException($"Event [{eventName}] does not exist in the emitter. Consider calling EventEmitter.On");
        }
        else
        {
            foreach (var f in subscribedMethods)
            {
                _ = f(eventName, data).ContinueWith(val =>
                {
                    val.Exception!.Handle(ex =>
                    {
                        Debug.WriteLine("Emit fail:{0}", ex);
                        return true;
                    });
                }, TaskContinuationOptions.OnlyOnFaulted);
            }
        }

        readerWriterLock.ExitReadLock();
    }

    /// <summary>
    /// Removes [method] from the event
    /// </summary>
    /// <param name="eventNames">Event name to remove function from</param>
    /// <param name="method">Method to remove from eventName</param>
    public void RemoveListener(string eventNames, Func<string, object?, Task> method)
    {
        readerWriterLock.EnterWriteLock();
        var eventNameList = eventNames.Split(EventSeparator, StringSplitOptions.RemoveEmptyEntries);
        foreach (var eventName in eventNameList)
        {
            if (!events.TryGetValue(eventName, out var subscribedMethods))
            {
                throw new DoesNotExistException($"Event [{eventName}] does not exist to have listeners removed.");
            }

            var @event = subscribedMethods.Exists(e => e == method);
            if (!@event)
            {
                throw new DoesNotExistException($"Func [{method.Method}] does not exist to be removed.");
            }

            subscribedMethods.Remove(method);
        }

        readerWriterLock.ExitWriteLock();
    }

    /// <summary>
    /// Removes all methods from the event [eventName]
    /// </summary>
    /// <param name="eventNames">Event name to remove methods from</param>
    public void RemoveAllListeners(string eventNames)
    {
        readerWriterLock.EnterWriteLock();
        var eventNameList = eventNames.Split(EventSeparator, StringSplitOptions.RemoveEmptyEntries);
        foreach (var eventName in eventNameList)
        {
            if (!events.TryGetValue(eventName, out var subscribedMethods))
            {
                throw new DoesNotExistException($"Event [{eventName}] does not exist to have methods removed.");
            }

            subscribedMethods.Clear();
        }

        readerWriterLock.ExitWriteLock();
    }
}
#endif