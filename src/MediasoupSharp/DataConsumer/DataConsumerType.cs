using System.Text.Json.Serialization;
// ReSharper disable InconsistentNaming

namespace MediasoupSharp.DataConsumer;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DataConsumerType
{
    sctp,
    direct
}