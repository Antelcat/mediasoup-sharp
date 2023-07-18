using System.Dynamic;

namespace MediasoupSharp;

using Events = IDictionary<string,List<object>>;

public class EnhancedEventEmitter<T> : EventEmitter where T : Events
{
    public override bool Emit(string eventName, params object[] args)
    {
        return base.Emit();
    }

    public override void On(string eventName, Action<object[]> listener)
    {
        throw new NotImplementedException();
    }

    public override void Off(string eventName, Action<object[]> listener)
    {
        throw new NotImplementedException();
    }

    public override void AddListener(string eventName, Action<object[]> listener)
    {
        throw new NotImplementedException();
    }
}