using System.Reflection;
using Antelcat.AutoGen.ComponentModel.Diagnostic;
using FBS.DirectTransport;
using FBS.SctpAssociation;
using FBS.SctpParameters;
using FBS.Transport;

namespace Antelcat.MediasoupSharp.Transport;

[AutoMetadataFrom(typeof(TransportBaseData), MemberTypes.Property,
    Leading = $"public {nameof(TransportBaseData)}(global::{nameof(FBS)}.{nameof(FBS.Transport)}.{nameof(DumpT)} dump){{",
    Template = "{Name} = dump.{Name};",
    Trailing = "}")]
[AutoMetadataFrom(typeof(TransportBaseData), MemberTypes.Property,
    Leading = $"public static implicit operator {nameof(TransportBaseData)}(global::{nameof(FBS)}.{nameof(DirectTransport)}.{nameof(DumpResponseT)} dump) => new(dump.Base); ")]
[AutoMetadataFrom(typeof(TransportBaseData), MemberTypes.Property,
    Leading = $"public static implicit operator global::{nameof(FBS)}.{nameof(FBS.Transport)}.{nameof(DumpT)}({nameof(TransportBaseData)} source) => new (){{",
    Template = "{Name} = source.{Name},",
    Trailing = "};")]
[AutoMetadataFrom(typeof(TransportBaseData), MemberTypes.Property,
    Leading = $"public static implicit operator {nameof(TransportBaseData)}(global::{nameof(FBS)}.{nameof(FBS.Transport)}.{nameof(DumpT)} dump) => new(dump);")]
public partial class TransportBaseData
{
    /// <summary>
    /// SCTP parameters.
    /// </summary>
    public SctpParametersT? SctpParameters { get; set; }

    /// <summary>
    /// Sctp state.
    /// </summary>
    public SctpState? SctpState { get; set; }
}