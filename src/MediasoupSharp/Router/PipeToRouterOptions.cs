﻿using MediasoupSharp.SctpParameters;
using MediasoupSharp.Transport;

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
    internal IRouter Router { get; set; }

    /// <summary>
    /// IP used in the PipeTransport pair. Default '127.0.0.1'.
    /// <see cref="TransportListenIp"/> or <see cref="string"/>
    /// </summary>
    public object? ListenIp { get; set; }

    /// <summary>
    /// Create a SCTP association. Default true.
    /// </summary>
    public bool? EnableSctp { get; set; } = true;

    /// <summary>
    /// SCTP streams number.
    /// </summary>
    public NumSctpStreams? NumSctpStreams { get; set; }

    /// <summary>
    /// Enable RTX and NACK for RTP retransmission.
    /// </summary>
    public bool? EnableRtx { get; set; } = false;

    /// <summary>
    /// Enable SRTP.
    /// </summary>
    public bool? EnableSrtp { get; set; } = false;
}