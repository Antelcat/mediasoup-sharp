using System.Collections.Concurrent;
using System.ComponentModel;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.Serialization;
using Antelcat.MediasoupSharp;
using Antelcat.MediasoupSharp.Internals.Converters;

namespace Antelcat.MediasoupSharp.Internals.Extensions;

internal static class EnumExtensions
{
    public static string GetEnumText<TEnum>(this TEnum @enum)
        where TEnum : Enum =>
        IEnumStringConverter.JsonConverters.OfType<EnumStringConverter<TEnum>>().FirstOrDefault() is { } converter
            ? converter.Convert(@enum) ?? throw new InvalidEnumArgumentException(@enum.ToString())
            : throw new EnumNotMappedException(typeof(TEnum));
}