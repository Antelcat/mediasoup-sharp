using System.Reflection;
using Antelcat.AutoGen.ComponentModel.Diagnostic;
using Antelcat.MediasoupSharp.Transport;
using FBS.Transport;
using FBS.WebRtcTransport;

namespace Antelcat.MediasoupSharp.WebRtcTransport;

[AutoMetadataFrom(typeof(WebRtcTransportData), MemberTypes.Property,
    Leading =
        $"public static implicit operator {nameof(WebRtcTransportData)}(global::{nameof(FBS)}.{nameof(FBS.WebRtcTransport)}.{nameof(DumpResponseT)} source) => new (source.Base){{",
    Template = "{Name} = source.{Name},",
    Trailing = "};")]
[AutoMetadataFrom(typeof(WebRtcTransportData), MemberTypes.Property,
    Leading =
        $"public static implicit operator global::{nameof(FBS)}.{nameof(FBS.WebRtcTransport)}.{nameof(DumpResponseT)}({nameof(WebRtcTransportData)} source) => new (){{",
    Template = "{Name} = source.{Name},",
    Trailing = "Base = source  };")]
public partial class WebRtcTransportData(DumpT dump) : TransportBaseData(dump)
{
    public required IceRole IceRole { get; set; }

    public required IceParametersT IceParameters { get; set; }

    public required List<IceCandidateT> IceCandidates { get; set; }

    public IceState IceState { get; set; }

    public TupleT? IceSelectedTuple { get; set; }

    public DtlsParametersT DtlsParameters { get; set; }

    public DtlsState DtlsState { get; set; }

    public string? DtlsRemoteCert;
}