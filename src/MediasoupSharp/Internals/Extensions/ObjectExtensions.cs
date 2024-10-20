using System.Text.Json;

namespace MediasoupSharp.Internals.Extensions;

internal static class ObjectExtensions
{
    public static bool IsStringType(this object o) => 
        o is JsonElement jsonElement 
            ? jsonElement.ValueKind == JsonValueKind.String 
            : o is string;

    public static bool IsNumericType(this object o)
    {
        return o is JsonElement jsonElement
            ? jsonElement.ValueKind == JsonValueKind.Number
            : o
                is byte or sbyte
                or short or ushort
                or int or uint
                or long or ulong
                or decimal
                or float or double;
    }
}