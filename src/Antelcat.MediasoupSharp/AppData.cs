namespace Antelcat.MediasoupSharp;

public class AppData(object? data = null)
{
    public object? Data { get; set; } = data ?? new Dictionary<string, object>();

    public T As<T>() => Data is T t ? t : throw new InvalidCastException($"data is not {typeof(T)}");

    public static implicit operator AppData(Dictionary<string, object> dictionary) => new(dictionary);

    public static implicit operator Dictionary<string, object>(AppData appData) =>
        appData.As<Dictionary<string, object>>();

    public object? this[string key]
    {
        get => As<IDictionary<string, object?>>()[key];
        set => As<IDictionary<string, object?>>()[key] = value;
    }

    public bool TryGetValue(string key, out object? value) =>
        As<IDictionary<string, object?>>().TryGetValue(key, out value);
}