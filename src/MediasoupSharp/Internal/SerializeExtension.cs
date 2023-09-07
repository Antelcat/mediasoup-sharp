using System.Text.Json;

namespace MediasoupSharp.Internal;

internal static class SerializeExtension
{
    public static T? Deserialize<T>(this string? str) => str == null
        ? default
        : JsonSerializer.Deserialize<T>(str, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = false
        });

    public static string Serialize<T>(this T target) => JsonSerializer.Serialize(target,
        new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
}