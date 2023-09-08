// ReSharper disable InconsistentNaming

using System.Text.Json.Serialization;

namespace MediasoupSharp.Producer;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ProducerType
{
    simple = 1,
    simulcast = 2,
    svc = 3
}