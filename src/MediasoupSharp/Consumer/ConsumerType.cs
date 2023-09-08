// ReSharper disable InconsistentNaming

using System.Text.Json.Serialization;

namespace MediasoupSharp.Consumer;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ConsumerType
{
    simple,
    simulcast,
    svc,
    pipe
}