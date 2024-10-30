using System.Diagnostics;
using System.Linq.Expressions;
using Antelcat.NodeSharp.Events;

namespace Antelcat.MediasoupSharp.EnhancedEvent;

public class EnhancedEventEmitter : EventEmitter
{
    public EnhancedEventEmitter()
    {
        EmitError += (eventName, exception) =>
        {
            Debugger.Break();
        };
    }
    public void On(string eventName, Func<object?, Task> method) =>
        base.On(eventName, (Func<object[], Task>)(args => method(args.Length > 0 ? args[0] : null)));
}

public class EnhancedEventEmitter<T>(EnhancedEventEmitter emitter) : IEnhancedEventEmitter<T>
{
    private readonly EnhancedEventEmitter emitter = emitter;
    // can be created
    public EnhancedEventEmitter() : this(new()) { }
    public static implicit operator EnhancedEventEmitter<T>(EnhancedEventEmitter emitter) => new(emitter);
    public static implicit operator EnhancedEventEmitter(EnhancedEventEmitter<T> emitter) => emitter.emitter;
    
    private static string GetMemberNameOrThrow<TProperty>(Expression<Func<T, TProperty>> eventName) =>
        eventName is not MemberExpression memberExpression
            ? throw new ArgumentOutOfRangeException(nameof(eventName))
            : memberExpression.Member.Name;

    public IEnhancedEventEmitter<T> On<TProperty>(Expression<Func<T, TProperty>> eventName,
                                                  Func<TProperty?, Task> method) =>
        (emitter.On(GetMemberNameOrThrow(eventName), method) as IEnhancedEventEmitter<T>)!;

    public IEnhancedEventEmitter<T> On<TProperty>(Expression<Func<T, TProperty>> eventName,
                                                  Action<TProperty?> method) =>
        (emitter.On(GetMemberNameOrThrow(eventName), method) as IEnhancedEventEmitter<T>)!;

    public void Emit<TProperty>(Expression<Func<T, TProperty>> eventName, TProperty? arg) =>
        emitter.Emit(GetMemberNameOrThrow(eventName), arg);
    
    public IEventEmitter AddListener(string eventName, Delegate listener) => emitter.AddListener(eventName, listener);
    public bool Emit(string eventName, params object?[] args) => emitter.Emit(eventName, args);
    public int ListenerCount(string eventName, Delegate? listener = null) => emitter.ListenerCount(eventName, listener);
    public IEnumerable<Delegate> Listeners(string eventName) => emitter.Listeners(eventName);
    public IEventEmitter Off(string eventName, Delegate listener) => emitter.Off(eventName, listener);
    public IEventEmitter On(string eventName, Delegate listener) => emitter.On(eventName, listener);
    public IEventEmitter Once(string eventName, Delegate listener) => emitter.Once(eventName, listener);
    public IEventEmitter PrependListener(string eventName, Delegate listener) => emitter.PrependListener(eventName, listener);
    public IEventEmitter PrependOnceListener(string eventName, Delegate listener) => emitter.PrependOnceListener(eventName, listener);
    public IEventEmitter RemoveAllListeners(string eventName) => emitter.RemoveAllListeners(eventName);
    public IEventEmitter RemoveListener(string eventName, Delegate listener) => emitter.RemoveListener(eventName, listener);
    public IEnumerable<Delegate> RawListeners(string eventName) => emitter.RawListeners(eventName);
    public IReadOnlyCollection<string> EventNames => emitter.EventNames;
    public int MaxListeners
    {
        get => emitter.MaxListeners;
        set => emitter.MaxListeners = value;
    }

    public event Action<string, Delegate>? NewListener
    {
        add => emitter.NewListener += value;
        remove => emitter.NewListener -= value;
    }

    public event Action<string, Delegate>? RemovedListener
    {
        add => emitter.RemovedListener += value;
        remove => emitter.RemovedListener -= value;
    }

    public event Action<string, Exception>? EmitError
    {
        add => emitter.EmitError += value;
        remove => emitter.EmitError -= value;
    }
}
