namespace MediasoupSharp;

public static partial class MediasoupSharp
{
    public static string WorkerBin { get; set; }

    internal static string? Env(string key) => Environment.GetEnvironmentVariable(key);
}