using System.Linq.Expressions;
using Antelcat.NodeSharp.Events;

namespace Antelcat.MediasoupSharp.EnhancedEvent;

public interface IEnhancedEventEmitter<T> : IEventEmitter
{
    public IEnhancedEventEmitter<T> On<TProperty>(Expression<Func<T, TProperty>> eventName,
                                                  Func<TProperty?, Task> method);

    public IEnhancedEventEmitter<T> On<TProperty>(Expression<Func<T, TProperty>> eventName,
                                                  Action<TProperty?> method);

    public void Emit<TProperty>(Expression<Func<T, TProperty>> eventName, TProperty? arg);
}
