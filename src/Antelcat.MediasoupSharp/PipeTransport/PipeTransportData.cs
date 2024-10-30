using System.Reflection;
using Antelcat.AutoGen.ComponentModel.Diagnostic;
using Antelcat.MediasoupSharp.Transport;
using FBS.PipeTransport;
using FBS.SrtpParameters;
using FBS.Transport;

namespace Antelcat.MediasoupSharp.PipeTransport;

[AutoMetadataFrom(typeof(PipeTransportData), MemberTypes.Property,
    Leading =
        $"public static implicit operator {nameof(PipeTransportData)}(global::{nameof(FBS)}.{nameof(FBS.PipeTransport)}.{nameof(DumpResponseT)} source) => new (source.Base){{",
    Template = "{Name} = source.{Name},",
    Trailing = "};")]
[AutoMetadataFrom(typeof(PipeTransportData), MemberTypes.Property,
    Leading =
        $"public static implicit operator global::{nameof(FBS)}.{nameof(FBS.PipeTransport)}.{nameof(DumpResponseT)}({nameof(PipeTransportData)} source) => new (){{",
    Template = "{Name} = source.{Name},",
    Trailing = "Base = source  };")]
public partial class PipeTransportData(DumpT dump) : TransportBaseData(dump)
{
    public TupleT Tuple { get; set; }

    public bool Rtx { get; set; }

    public SrtpParametersT? SrtpParameters { get; set; }
}