using System.Text.RegularExpressions;

namespace MediasoupSharp;

public partial class ScalabilityModeRegex
{
    public static readonly Regex Regex = MyRegex();

    [GeneratedRegex("^[LS]([1-9]\\d{0,1})T([1-9]\\d{0,1})(_KEY)?")]
    private static partial Regex MyRegex();
}

public record ScalabilityMode(
    Number SpatialLayers,
    Number TemporalLayers,
    bool Ksvc)
{
    static ScalabilityMode Parse(string? scalabilityMode)
    {
        var match = ScalabilityModeRegex.Regex.Matches(scalabilityMode ?? string.Empty);
        if (match.Count > 0)
        {
            return new(
                int.Parse(match[1].Value),
                int.Parse(match[2].Value),
                bool.Parse(match[3].Value));
        }
        return new ScalabilityMode(1, 1, false);
    }
}