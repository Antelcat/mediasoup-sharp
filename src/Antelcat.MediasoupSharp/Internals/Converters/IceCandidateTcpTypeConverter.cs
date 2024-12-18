﻿using Antelcat.MediasoupSharp.FBS.WebRtcTransport;

namespace Antelcat.MediasoupSharp.Internals.Converters;

internal class IceCandidateTcpTypeConverter : EnumStringConverter<IceCandidateTcpType>
{
    protected override IEnumerable<(IceCandidateTcpType Enum, string Text)> Map()
    {
        yield return (IceCandidateTcpType.PASSIVE, "passive");
    }
}