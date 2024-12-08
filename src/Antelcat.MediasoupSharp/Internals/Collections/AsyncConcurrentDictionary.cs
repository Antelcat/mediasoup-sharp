using System.Collections;
using System.Diagnostics.CodeAnalysis;
using Microsoft.VisualStudio.Threading;

namespace Antelcat.MediasoupSharp.Internals.Collections;

public class AsyncConcurrentDictionary<TKey, TValue> : IReadOnlyDictionary<TKey, TValue> where TKey : class
{
    private readonly Dictionary<TKey, TValue> dictionary = [];
    private readonly AsyncAutoResetEvent      locker     = new();

    /// <summary>
    /// <see cref="AsyncAutoResetEvent.Set"/>
    /// </summary>
    public void Set() => locker.Set();
    public async Task ModifyAsync(Action<Dictionary<TKey, TValue>> action)
    {
        await locker.WaitAsync();
        try
        {
            action(dictionary);
        }
        finally
        {
            locker.Set();
        }
    }
    public async Task ModifyAsync(Func<Dictionary<TKey, TValue>, Task> action)
    {
        await locker.WaitAsync();
        try
        {
            await action(dictionary);
        }
        finally
        {
            locker.Set();
        }
    }

    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => dictionary.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)dictionary).GetEnumerator();

    public int Count => dictionary.Count;

    public bool ContainsKey(TKey key) => dictionary.ContainsKey(key);

    public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value) => dictionary.TryGetValue(key, out value);

    public TValue this[TKey key] => dictionary[key];

    public IEnumerable<TKey> Keys => dictionary.Keys;

    public IEnumerable<TValue> Values => dictionary.Values;
}