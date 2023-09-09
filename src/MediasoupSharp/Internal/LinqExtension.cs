namespace MediasoupSharp.Internal;

internal static class LinqExtension
{
    public static void ForEach<T>(this ICollection<T> source, Action<T> action)
    {
        foreach (var item in source)
        {
            action(item);
        }
    }

    public static bool IsNullOrEmpty<T>(this IEnumerable<T>? source) => source == null || !source.Any();

    public static IDictionary<TK, TV> Merge<TK, TV>(this IDictionary<TK, TV> dictionary1,
        IDictionary<TK, TV> dictionary2) where TK : notnull
    {
        return dictionary2.Aggregate(
            dictionary1.ToDictionary(x => x.Key, x => x.Value),
            (s, p) =>
            {
                s.Add(p.Key, p.Value);
                return s;
            });
    }
}