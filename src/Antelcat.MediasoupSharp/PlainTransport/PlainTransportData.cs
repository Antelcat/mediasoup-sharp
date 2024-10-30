using System.Reflection;
using Antelcat.AutoGen.ComponentModel.Diagnostic;
using Antelcat.MediasoupSharp.Transport;
using FBS.PlainTransport;
using FBS.SrtpParameters;
using FBS.Transport;

namespace Antelcat.MediasoupSharp.PlainTransport;

[AutoMetadataFrom(typeof(PlainTransportData), MemberTypes.Property,
    Leading =
        $"public static implicit operator {nameof(PlainTransportData)}(global::{nameof(FBS)}.{nameof(FBS.PlainTransport)}.{nameof(DumpResponseT)} source) => new (source.Base){{",
    Template = "{Name} = source.{Name},",
    Trailing = "};")]
[AutoMetadataFrom(typeof(PlainTransportData), MemberTypes.Property,
    Leading =
        $"public static implicit operator global::{nameof(FBS)}.{nameof(FBS.PlainTransport)}.{nameof(DumpResponseT)}({nameof(PlainTransportData)} source) => new (){{",
    Template = "{Name} = source.{Name},",
    Trailing = "Base = source  };")]
public partial class PlainTransportData(DumpT dump) : TransportBaseData(dump)
{
    public bool RtcpMux { get; set; }

    public bool Comedia { get; set; }

    public TupleT Tuple { get; set; }

    public TupleT? RtcpTuple { get; set; }

    public SrtpParametersT? SrtpParameters { get; set; }
}