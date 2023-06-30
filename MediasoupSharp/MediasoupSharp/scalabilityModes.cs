using System.Text.RegularExpressions;

namespace MediasoupSharp;

public partial class ScalabilityModeRegex
{
    public static readonly Regex Regex = MyRegex();

    [GeneratedRegex("^[LS]([1-9]\\d{0,1})T([1-9]\\d{0,1})(_KEY)?")]
    private static partial Regex MyRegex();
}

public record ScalabilityMode(
    Number spatialLayers,
    Number temporalLayers,
    bool ksvc)
{
    static ScalabilityMode parse(string? scalabilityMode)
    {
        var match = ScalabilityModeRegex.Regex.Matches(scalabilityMode ?? string.Empty);
        if (match.Count > 0)
        {
            return new(
                match[1].Value.parseInt(),
                match[2].Value.parseInt(), 
                bool.Parse(match[3].Value));
        }
        return new ScalabilityMode(1, 1, false);
    }
}