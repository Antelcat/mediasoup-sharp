using System.Text.Json.Serialization;
// ReSharper disable InconsistentNaming

namespace MediasoupSharp.Consumer;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ConsumerTraceEventType
{
    rtp,
    keyframe,
    nack,
    pli,
    fir
}