using System.Text.Json;

namespace MediasoupSharp.Internal;

public static class SerializeExtension
{
    public static T? Deserialize<T>(this string? str) => str == null ? default : JsonSerializer.Deserialize<T>(str);
}