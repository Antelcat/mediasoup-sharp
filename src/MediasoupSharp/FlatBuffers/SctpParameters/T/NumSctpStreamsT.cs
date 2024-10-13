using System.Text.Json.Serialization;

namespace MediasoupSharp.FlatBuffers.SctpParameters.T;

public class NumSctpStreamsT
{
    /// <summary>
    /// OS. Renamed.
    /// </summary>
    [JsonPropertyName("OS")]
    public ushort OS { get; set; }

    /// <summary>
    /// MIS. Renamed.
    /// </summary>
    [JsonPropertyName("MIS")]
    public ushort MIS { get; set; }
}