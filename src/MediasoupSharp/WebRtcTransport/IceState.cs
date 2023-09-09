// ReSharper disable InconsistentNaming

using System.Text.Json.Serialization;

namespace MediasoupSharp.WebRtcTransport;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum IceState
{
    @new,
    connected,
    completed,
    disconnected,
    closed
}