using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace MediasoupSharp.ClientRequest;

public class JoinRoomRequest
{
    public string RoomId { get; set; }

    public UserRole Role { get; set; }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum UserRole
{
    [EnumMember(Value = "normal")]
    Normal,

    [EnumMember(Value = "admin")]
    Admin
}
