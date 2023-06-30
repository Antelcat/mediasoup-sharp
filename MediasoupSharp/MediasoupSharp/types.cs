using System.Reflection.Metadata.Ecma335;

namespace MediasoupSharp;

public abstract class AppData
{
    public abstract object? this[string key] { get; set; }
}

public static class ES
{
    public static int parseInt(this string? str) => int.Parse(str ?? string.Empty);
}