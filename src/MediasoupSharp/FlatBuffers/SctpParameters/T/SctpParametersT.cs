using System.Text.Json.Serialization;

namespace MediasoupSharp.FlatBuffers.SctpParameters.T;

public class SctpParametersT
{
    public ushort Port { get; set; } = 5000;

    /// <summary>
    /// OS. Renamed.
    /// </summary>
    [JsonPropertyName("OS")]
    public ushort Os { get; set; }

    /// <summary>
    /// MIS. Renamed.
    /// </summary>
    [JsonPropertyName("MIS")]
    public ushort Mis { get; set; }

    public uint MaxMessageSize { get; set; }

    public uint SendBufferSize { get; set; }

    public uint SctpBufferedAmount { get; set; }

    public bool IsDataChannel { get; set; }
}