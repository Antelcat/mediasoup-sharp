using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Xml;

namespace Antelcat.MediasoupSharp.Internals.Extensions;

internal static class ObjectExtensions
{
    public static T Sure<T>(this object obj) where T : class => obj as T ?? throw new InvalidCastException();
    
    public static bool IsStringType(this object o) =>
        o is JsonElement jsonElement
            ? jsonElement.ValueKind == JsonValueKind.String
            : o is string;

    public static bool IsNumericType(this object o) =>
        o is JsonElement jsonElement
            ? jsonElement.ValueKind == JsonValueKind.Number
            : o
                is byte or sbyte
                or short or ushort
                or int or uint
                or long or ulong
                or decimal
                or float or double;

    [return: NotNullIfNotNull(nameof(obj))]
    public static T? DeepClone<
        [DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.PublicConstructors |
            DynamicallyAccessedMemberTypes.NonPublicConstructors)]
        T>(this T? obj) => (T?)DeepClone(obj, obj?.GetType());

    public static object? DeepClone(this object? obj,
                                    [DynamicallyAccessedMembers(
                                        DynamicallyAccessedMemberTypes.PublicConstructors    |
                                        DynamicallyAccessedMemberTypes.NonPublicConstructors |
                                        DynamicallyAccessedMemberTypes.PublicFields          |
                                        DynamicallyAccessedMemberTypes.NonPublicFields
                                    )]
                                    Type? type) =>
        obj is null
            ? default
            : type == typeof(object)
                ? new object()
                : IsSystemValueType(obj)
                    ? obj
                    : GetOrAddMapper(type ?? obj.GetType())(obj);


    private static readonly Dictionary<Type, Func<object, object>> Mappers = [];

    private static Func<object, object> GetOrAddMapper(
        [DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.PublicConstructors    |
            DynamicallyAccessedMemberTypes.NonPublicConstructors |
            DynamicallyAccessedMemberTypes.PublicFields          |
            DynamicallyAccessedMemberTypes.NonPublicFields
        )]
        Type type)
    {
        if (Mappers.TryGetValue(type, out var mapper)) return mapper;

        if (type.IsArray) return GetArrayMapper(type.GetElementType()!);

        // is {}
        var fields = type.GetAllFields();
        Func<object, object> handle = arg =>
        {
            var ret = RuntimeHelpers.GetUninitializedObject(type);
            foreach (var field in fields)
            {
                var value = field.GetValue(arg);
                field.SetValue(ret, value?.DeepClone(value.GetType()));
            }

            return ret;
        };
        Mappers[type] = handle;
        return handle;
    }

    private static bool IsSystemValueType(object obj) => obj
        is bool
        or byte or sbyte
        or char or string
        or short or ushort
        or int or uint
        or long or ulong
        or decimal
        or float or double
        or uint or nint or nuint
        or Enum or DateTime or Guid;

    private static Func<object, object> GetArrayMapper(Type elementType) =>
        obj =>
        {
            var arr    = (obj as Array)!;
            var length = arr.Length;
            var array  = Array.CreateInstance(elementType, length);
            for (var i = 0; i < length; i++)
            {
                var value = arr.GetValue(i);
                array.SetValue(value?.DeepClone(value.GetType()), i);
            }

            return array;
        };

    private static IEnumerable<FieldInfo> GetAllFields(this Type? type)
    {
        while (type != null && type != typeof(object))
        {
            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                yield return field;
            }
            type = type.BaseType;
        }
    }
}