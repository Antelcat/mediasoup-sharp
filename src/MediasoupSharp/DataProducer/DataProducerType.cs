// ReSharper disable InconsistentNaming

using System.Text.Json.Serialization;

namespace MediasoupSharp.DataProducer;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DataProducerType
{
    sctp,
    direct
}