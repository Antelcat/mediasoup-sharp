using System.Collections;
using System.Dynamic;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.Json;
using System.Xml.Serialization;

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

    public static T DeepClone<T>(this T target)
    {
        object? retval;
        using var ms = new MemoryStream();
        {
            var xml = new XmlSerializer(typeof(T));
            xml.Serialize(ms, target);
            ms.Seek(0, SeekOrigin.Begin);
            retval = xml.Deserialize(ms);
            ms.Close();
        }
        return (T)retval!;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ExpandoObject CopyToExpandoObject(this IDictionary<string,object?> dictionary)
    {
        var ret = new ExpandoObject();
        foreach (var (key,value) in dictionary)
        {
            ret.TryAdd(key, value);
        }
        return ret;
    }
}