using System.Text.Json.Serialization;
// ReSharper disable InconsistentNaming

namespace MediasoupSharp.RtpParameters;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum MediaKind
{
    audio,
    video
}