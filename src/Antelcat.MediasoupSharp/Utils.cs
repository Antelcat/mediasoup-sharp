namespace Antelcat.MediasoupSharp;

internal static class Utils
{
    private static readonly Random Random = new();

    /// <summary>
    /// 生成 100000000 - 999999999 的随机数
    /// </summary>
    /// <returns></returns>
    public static uint GenerateRandomNumber()
    {
        return (uint)Random.Next(100_000_000, 1_000_000_000);
    }
}