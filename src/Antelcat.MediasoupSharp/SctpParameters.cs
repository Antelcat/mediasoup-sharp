using System.Reflection;
using System.Text.Json.Serialization;
using Antelcat.AutoGen.ComponentModel.Diagnostic;
using FBS.SctpParameters;

namespace Antelcat.MediasoupSharp;

public class SctpCapabilities
{
    public NumSctpStreamsT NumStreams { get; set; }
}

[AutoMetadataFrom(typeof(SctpStreamParameters), MemberTypes.Property,
    Leading =
        $"public static implicit operator {nameof(SctpStreamParameters)}(global::{nameof(FBS)}.{nameof(SctpParameters)}.{nameof(SctpStreamParametersT)}? param) => param is null ? new() : new (){{",
    Template = "{Name} = param.{Name},",
    Trailing = "};")]
[AutoMetadataFrom(typeof(SctpStreamParameters), MemberTypes.Property,
    Leading =
        $"public static implicit operator global::{nameof(FBS)}.{nameof(SctpParameters)}.{nameof(SctpStreamParametersT)}({nameof(SctpStreamParameters)}? param) => param is null ? new() : new (){{",
    Template = "{Name} = param.{Name},",
    Trailing = "};")]
public partial record SctpStreamParameters
{
    public ushort StreamId { get; set; }
    public bool?  Ordered  { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ushort? MaxPacketLifeTime { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ushort? MaxRetransmits { get; set; }
}