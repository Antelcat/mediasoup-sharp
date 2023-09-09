using System.Text.RegularExpressions;

namespace MediasoupSharp;

public partial class ScalabilityMode
{

    public int  SpatialLayers  { get; set; }
    public int  TemporalLayers { get; set; }
    public bool Ksvc           { get; set; }

    public static ScalabilityMode Parse(string? scalabilityMode)
    {
        var match = Regex().Matches(scalabilityMode ?? string.Empty);
        if (match.Count > 0)
        {
            return new()
            {
                SpatialLayers  = int.Parse(match[1].Value),
                TemporalLayers = int.Parse(match[2].Value),
                Ksvc           = bool.Parse(match[3].Value)
            };
        }

        return new()
        {
            SpatialLayers  = 1,
            TemporalLayers = 1,
            Ksvc           = false
        };
    }

    [GeneratedRegex("^[LS]([1-9]\\\\d{0,1})T([1-9]\\\\d{0,1})(_KEY)?")]
    private static partial Regex Regex();
}