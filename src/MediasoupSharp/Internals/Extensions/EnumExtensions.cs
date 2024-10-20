using System.Collections.Concurrent;
using System.ComponentModel;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.Serialization;
using MediasoupSharp.Exceptions;
using MediasoupSharp.Internals.Converters;

namespace MediasoupSharp.Internals.Extensions;

internal static class EnumExtensions
{
    public static string GetEnumText<TEnum>(this TEnum @enum)
        where TEnum : Enum =>
        IEnumStringConverter.Converters().OfType<EnumStringConverter<TEnum>>().FirstOrDefault() is { } converter
            ? converter.Convert(@enum) ?? throw new InvalidEnumArgumentException(@enum.ToString())
            : throw new EnumNotMappedException(typeof(TEnum));
}