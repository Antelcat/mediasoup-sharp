using System.Collections.Concurrent;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.Serialization;

namespace MediasoupSharp.Internals.Extensions;

internal static class EnumExtensions
{
    private static readonly ConcurrentDictionary<Type, ConcurrentDictionary<Enum, string>> Cache = [];

    public static string GetEnumMemberValue<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)]
        TEnum>(this TEnum @enum)
        where TEnum : Enum =>
        Cache
            .GetOrAdd(typeof(TEnum), static _ => [])
            .GetOrAdd(@enum, static i => ((TEnum)i)
                .GetEnumAttribute<TEnum, EnumMemberAttribute>()
                .FirstOrDefault()?
                .NamedArguments
                .FirstOrDefault()
                .TypedValue.Value as string ?? throw new NoNullAllowedException(nameof(EnumMemberAttribute)));

    private static IEnumerable<CustomAttributeData> GetEnumAttribute<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)]
        TEnum, TAttribute>(this TEnum @enum)
        where TEnum : Enum
        where TAttribute : Attribute =>
        typeof(TEnum)
            .GetField(@enum.ToString())?
            .CustomAttributes
            .Where(x => x.AttributeType == typeof(TAttribute)) ?? [];
}