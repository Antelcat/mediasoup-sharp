using System.Diagnostics.CodeAnalysis;

namespace MediasoupSharp.Extensions;

internal static class EnumerableExtensions
{
    public static bool IsNullOrEmpty<T>([NotNullWhen(false)] this IEnumerable<T>? enumerable) =>
        enumerable is null || !enumerable.Any();
    
    public static Dictionary<TKey, TValue> Merge<TKey, TValue>(this IDictionary<TKey, TValue> first, IDictionary<TKey, TValue> second)
        where TKey : notnull
    {
        var result = new Dictionary<TKey, TValue>();
        foreach (var key in first.Keys)
        {
            result[key] = first[key];
        }

        foreach (var key in second.Keys)
        {
            result[key] = second[key];
        }

        return result;
    }
    
    public static bool DeepEquals<TKey, TValue>(this IDictionary<TKey, TValue> first, IDictionary<TKey, TValue> second)
        where TKey : notnull
    {
        var comparer = new DictionaryComparer<TKey, TValue>();
        return comparer.Equals(first, second);
    }

    private class DictionaryComparer<TKey, TValue> : IEqualityComparer<IDictionary<TKey, TValue>> where TKey : notnull
    {
        public bool Equals(IDictionary<TKey, TValue>? x, IDictionary<TKey, TValue>? y)
        {
            ArgumentNullException.ThrowIfNull(x);

            ArgumentNullException.ThrowIfNull(y);

            if (x.Count != y.Count)
            {
                return false;
            }

            foreach (var kvp in x)
            {
                if (!y.TryGetValue(kvp.Key, out var value))
                {
                    return false;
                }

                if ((value == null && kvp.Value != null) || (value != null && kvp.Value == null) ||
                    (value != null && kvp.Value != null                    && !value.Equals(kvp.Value)))
                {
                    return false;
                }
            }

            return true;
        }

        public int GetHashCode(IDictionary<TKey, TValue> obj)
        {
            ArgumentNullException.ThrowIfNull(obj);

            var hash = 0;
            foreach (var kvp in obj)
            {
                hash ^= kvp.Key.GetHashCode();
                if (kvp.Value != null)
                {
                    hash ^= kvp.Value.GetHashCode();
                }
            }

            return hash;
        }
    }
    
    public static int DeepGetHashCode<TKey, TValue>(this IDictionary<TKey, TValue> dic)
        where TKey : notnull =>
        new DictionaryComparer<TKey, TValue>().GetHashCode(dic);
}