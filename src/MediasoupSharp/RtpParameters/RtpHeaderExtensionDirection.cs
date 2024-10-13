using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace MediasoupSharp.RtpParameters;

/// <summary>
/// Direction of RTP header extension.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RtpHeaderExtensionDirection
{
    [EnumMember(Value = "sendrecv")]
    SendReceive,

    [EnumMember(Value = "sendonly")]
    SendOnly,

    [EnumMember(Value = "recvonly")]
    ReceiveOnly,

    [EnumMember(Value = "inactive")]
    Inactive
}