using System.Text.Json.Serialization;
// ReSharper disable InconsistentNaming

namespace MediasoupSharp.Producer;


[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ProducerTraceEventType
{
    rtp,
    keyframe,
    nack,
    pli,
    fir
}