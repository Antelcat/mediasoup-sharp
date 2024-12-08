using System.Collections;
using System.Diagnostics.CodeAnalysis;
using Microsoft.VisualStudio.Threading;

namespace Antelcat.MediasoupSharp.Internals.Collections;

public class AsyncReadWriteDictionary<TKey, TValue> : IDictionary<TKey, TValue> where TKey : class
{
    private readonly Dictionary<TKey, TValue> dictionary = new();
    private readonly AsyncReaderWriterLock    locker     = new(null);

    /// <summary>
    /// <see cref="AsyncReaderWriterLock.WriteLockAsync(CancellationToken)"/>
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public AsyncReaderWriterLock.Awaitable WriteLockAsync(CancellationToken cancellationToken = default) =>
        locker.WriteLockAsync(cancellationToken);
    
    /// <summary>
    /// <see cref="AsyncReaderWriterLock.ReadLockAsync"/>
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public AsyncReaderWriterLock.Awaitable ReadLockAsync(CancellationToken cancellationToken = default) =>
        locker.ReadLockAsync(cancellationToken);

    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => dictionary.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)dictionary).GetEnumerator();

    public void Add(KeyValuePair<TKey, TValue> item) => (dictionary as IDictionary<TKey, TValue>).Add(item);

    public void Clear() => dictionary.Clear();

    public bool Contains(KeyValuePair<TKey, TValue> item) => dictionary.Contains(item);

    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) =>
        (dictionary as IDictionary<TKey, TValue>).CopyTo(array, arrayIndex);

    public bool Remove(KeyValuePair<TKey, TValue> item) => (dictionary as IDictionary<TKey, TValue>).Remove(item);

    public int Count => dictionary.Count;

    public bool IsReadOnly => (dictionary as IDictionary<TKey, TValue>).IsReadOnly;

    public void Add(TKey key, TValue value) => dictionary.Add(key, value);

    public bool ContainsKey(TKey key) => dictionary.ContainsKey(key);

    public bool Remove(TKey key) => dictionary.Remove(key);

    public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value) =>
        dictionary.TryGetValue(key, out value);

    public TValue this[TKey key]
    {
        get => dictionary[key];
        set => dictionary[key] = value;
    }

    public ICollection<TKey> Keys => dictionary.Keys;

    public ICollection<TValue> Values => dictionary.Values;

    public TValue? GetValueOrDefault(TKey key) => dictionary.GetValueOrDefault(key);
}