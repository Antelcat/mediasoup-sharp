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
}