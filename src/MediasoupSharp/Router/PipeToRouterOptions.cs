﻿using MediasoupSharp.FlatBuffers.SctpParameters.T;
using MediasoupSharp.FlatBuffers.Transport.T;

namespace MediasoupSharp.Router;

public class PipeToRouterOptions
{
    /// <summary>
    /// The id of the Producer to consume.
    /// </summary>
    public string? ProducerId { get; set; }

    /// <summary>
    /// The id of the DataProducer to consume.
    /// </summary>
    public string? DataProducerId { get; set; }

    /// <summary>
    /// Target Router instance.
    /// </summary>
    public Router Router { get; set; }

    /// <summary>
    /// Listenning Infomation.
    /// </summary>
    public ListenInfoT ListenInfo { get; set; }

    /// <summary>
    /// Create a SCTP association. Default true.
    /// </summary>
    public bool EnableSctp { get; set; } = true;

    /// <summary>
    /// SCTP streams number.
    /// </summary>
    public NumSctpStreamsT NumSctpStreams { get; set; } = new() { OS = 1024, MIS = 1024 };

    /// <summary>
    /// Enable RTX and NACK for RTP retransmission.
    /// </summary>
    public bool EnableRtx { get; set; }

    /// <summary>
    /// Enable SRTP.
    /// </summary>
    public bool EnableSrtp { get; set; }
}
