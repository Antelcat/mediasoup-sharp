﻿namespace MediasoupSharp.DataConsumer;

public class DataConsumerOptions<TDataConsumerAppData>
{
    /// <summary>
    /// The id of the DataProducer to consume.
    /// </summary>
    public string DataProducerId { get; set; } = string.Empty;

    /// <summary>
    /// Just if consuming over SCTP.
    /// Whether data messages must be received in order. If true the messages will
    /// be sent reliably. Defaults to the value in the DataProducer if it has type
    /// 'sctp' or to true if it has type 'direct'.
    /// </summary>
    public bool? Ordered { get; set; }

    /// <summary>
    /// Just if consuming over SCTP.
    /// When ordered is false indicates the time (in milliseconds) after which a
    /// SCTP packet will stop being retransmitted. Defaults to the value in the
    /// DataProducer if it has type 'sctp' or unset if it has type 'direct'.
    /// </summary>
    public int? MaxPacketLifeTime { get; set; }

    /// <summary>
    /// Just if consuming over SCTP.
    /// When ordered is false indicates the maximum number of times a packet will
    /// be retransmitted. Defaults to the value in the DataProducer if it has type
    /// 'sctp' or unset if it has type 'direct'.
    /// </summary>
    public int? MaxRetransmits { get; set; }

    /// <summary>
    /// Custom application data.
    /// </summary>
    public TDataConsumerAppData? AppData { get; set; }
}