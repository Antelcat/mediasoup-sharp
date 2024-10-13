﻿using FlatBuffers.RtpParameters;
using Type = FlatBuffers.RtpParameters.Type;

namespace MediasoupSharp.Consumer;

public class ConsumerData
{
    /// <summary>
    /// Associated Producer id.
    /// </summary>
    public string ProducerId { get; set; }

    /// <summary>
    /// Media kind.
    /// </summary>
    public MediaKind Kind { get; set; }

    /// <summary>
    /// RTP parameters.
    /// </summary>
    public RtpParameters.RtpParameters RtpParameters { get; set; }

    /// <summary>
    /// Consumer type.
    /// </summary>
    public Type Type { get; set; }
}
