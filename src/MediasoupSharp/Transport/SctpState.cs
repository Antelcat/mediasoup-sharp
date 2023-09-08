// ReSharper disable InconsistentNaming

using System.Text.Json.Serialization;

namespace MediasoupSharp.Transport;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SctpState
{
    @new,
    connecting,
    connected,
    failed,
    closed
}