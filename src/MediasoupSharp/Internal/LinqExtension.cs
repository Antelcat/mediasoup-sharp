namespace MediasoupSharp.Internal;

internal static class LinqExtension
{
    public static void ForEach<T>(this ICollection<T> source,Action<T> action)
    {
        foreach (var item in source)
        {
            action(item);
        }
    }

    public static bool IsNullOrEmpty<T>(this IEnumerable<T>? source) => source == null || !source.Any();
}