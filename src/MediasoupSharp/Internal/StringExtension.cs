namespace MediasoupSharp.Internal;

internal static class StringExtension
{
    public static bool IsNullOrEmpty(this string? str) => string.IsNullOrEmpty(str);
    
    public static bool IsNullOrWhiteSpace(this string? str) => string.IsNullOrWhiteSpace(str);
}