// ReSharper disable InconsistentNaming
// ReSharper disable IdentifierTypo

using System.Text.Json.Serialization;

namespace MediasoupSharp.RtpParameters;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RtpHeaderExtensionDirection
{
    sendrecv,
    sendonly,
    recvonly,
    inactive
}