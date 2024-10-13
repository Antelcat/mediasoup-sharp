using System.Text.Json.Serialization;

namespace MediasoupSharp.FlatBuffers.WebRtcServer.T;

public class TupleHashT
{
    [JsonPropertyName("tupleHash")]
    public ulong TupleHash_ { get; set; }

    public string WebRtcTransportId { get; set; }
}