﻿using System.Diagnostics.CodeAnalysis;

namespace Antelcat.MediasoupSharp.Internals.Extensions;

internal static class StringExtensions
{
    /// <summary>
    /// <inheritdoc cref="string.IsNullOrWhiteSpace"/>
    /// </summary>
    /// <param name="value"><inheritdoc cref="string.IsNullOrWhiteSpace"/></param>
    /// <returns><inheritdoc cref="string.IsNullOrWhiteSpace"/></returns>
    public static bool IsNullOrWhiteSpace([NotNullWhen(false)] this string? value) => string.IsNullOrWhiteSpace(value);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static bool IsNullOrEmpty([NotNullWhen(false)] this string? value) => string.IsNullOrEmpty(value);
    
    public static string NullOrWhiteSpaceThen(this string? source, string newValue) => !string.IsNullOrWhiteSpace(source) ? source : newValue;
}