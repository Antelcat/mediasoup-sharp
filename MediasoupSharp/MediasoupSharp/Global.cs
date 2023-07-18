global using Number = System.Int32;

public abstract class EventEmitter
{
    public abstract bool Emit(string eventName, params object[] args);

    public abstract void On(string eventName, Action<object[]> listener);
    public abstract void Off(string eventName, Action<object[]> listener);
    public abstract void AddListener(string eventName, Action<object[]> listener);
}