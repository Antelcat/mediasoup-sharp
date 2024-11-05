global using static Antelcat.MediasoupSharp.EnhancedEventEmitterExtensions;
using System.Linq.Expressions;
using Antelcat.NodeSharp.Events;

namespace Antelcat.MediasoupSharp;

public class EnhancedEventEmitter : EventEmitter
{
    public void On(string eventName, Func<object?, Task> method) =>
        base.On(eventName, (Func<object[], Task>)(args => method(args.Length > 0 ? args[0] : null)));
}

public class EnhancedEventEmitter<T>(EnhancedEventEmitter emitter) : IEnhancedEventEmitter<T>
{
    private readonly EnhancedEventEmitter emitter = emitter;

    public IEventEmitter EventEmitter => emitter;
    // can be created
    public EnhancedEventEmitter() : this(new ()) { }
    public static implicit operator EnhancedEventEmitter<T>(EnhancedEventEmitter emitter) => new(emitter);
    public static implicit operator EnhancedEventEmitter(EnhancedEventEmitter<T> emitter) => emitter.emitter;
}

public interface IEnhancedEventEmitter<out T>
{
    internal IEventEmitter EventEmitter { get; }
}

public static class EnhancedEventEmitterExtensions
{
    private static string GetMemberNameOrThrow<T, TProperty>(Expression<Func<T, TProperty>> eventName) =>
        eventName switch
        {
            MemberExpression member                                        => member.Member.Name,
            { Body: MemberExpression member }                              => member.Member.Name,
            { Body: UnaryExpression { Operand: MemberExpression member } } => member.Member.Name,

            _ => throw new ArgumentOutOfRangeException(nameof(eventName))
        };

    public static IEnhancedEventEmitter<T> On<T, TProperty>(this IEnhancedEventEmitter<T> emitter,
                                                            Expression<Func<T, TProperty>> eventName,
                                                            Action method)
    {
        emitter.EventEmitter.On(GetMemberNameOrThrow(eventName), method);
        return emitter;
    }
    
    public static IEnhancedEventEmitter<T> On<T, TProperty>(this IEnhancedEventEmitter<T> emitter,
                                                            Expression<Func<T, TProperty>> eventName,
                                                            Func<Task> method)
    {
        emitter.EventEmitter.On(GetMemberNameOrThrow(eventName), method);
        return emitter;
    }
    
    public static IEnhancedEventEmitter<T> On<T, TProperty>(this EnhancedEventEmitter<T> emitter,
                                                            Expression<Func<T, TProperty>> eventName,
                                                            Action method)
    {
        emitter.EventEmitter.On(GetMemberNameOrThrow(eventName), method);
        return emitter;
    }
    
    public static IEnhancedEventEmitter<T> On<T, TProperty>(this EnhancedEventEmitter<T> emitter,
                                                            Expression<Func<T, TProperty>> eventName,
                                                            Func<Task> method)
    {
        emitter.EventEmitter.On(GetMemberNameOrThrow(eventName), method);
        return emitter;
    }

    
    public static IEnhancedEventEmitter<T> On<T, TProperty>(this IEnhancedEventEmitter<T> emitter,
                                                            Expression<Func<T, TProperty>> eventName,
                                                            Func<TProperty, Task> method)
    {
        emitter.EventEmitter.On(GetMemberNameOrThrow(eventName), method);
        return emitter;
    }

    public static IEnhancedEventEmitter<T> On<T, TProperty>(this IEnhancedEventEmitter<T> emitter,
                                                            Expression<Func<T, TProperty>> eventName,
                                                            Action<TProperty> method)
    {
         emitter.EventEmitter.On(GetMemberNameOrThrow(eventName), method);
         return emitter;
    }

    public static IEnhancedEventEmitter<T> On<T, TProperty>(this EnhancedEventEmitter<T> emitter,
                                                            Expression<Func<T, TProperty>> eventName,
                                                            Func<TProperty, Task> method)
    {
        emitter.EventEmitter.On(GetMemberNameOrThrow(eventName), method);
        return emitter;
    }
    
    
    public static IEnhancedEventEmitter<T> On<T, TProperty>(this EnhancedEventEmitter<T> emitter,
                                                            Expression<Func<T, TProperty>> eventName,
                                                            Action<TProperty> method)
    {
        emitter.EventEmitter.On(GetMemberNameOrThrow(eventName), method);
        return emitter;
    }
    
    public static void Emit<T, TProperty>(this IEnhancedEventEmitter<T> emitter,
                                          Expression<Func<T, TProperty>> eventName, TProperty? arg = default) =>
        emitter.EventEmitter.Emit(GetMemberNameOrThrow(eventName), arg);
    
    public static void Emit<T, TProperty>(this EnhancedEventEmitter<T> emitter,
                                          Expression<Func<T, TProperty>> eventName, TProperty? arg = default) =>
        emitter.EventEmitter.Emit(GetMemberNameOrThrow(eventName), arg);
}