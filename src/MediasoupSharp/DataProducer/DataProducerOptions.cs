﻿using MediasoupSharp.SctpParameters;

namespace MediasoupSharp.DataProducer;

public class DataProducerOptions<TDataProducerAppData>
{
    /// <summary>
    /// DataProducer id (just for Router.pipeToRouter() method).
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// SCTP parameters defining how the endpoint is sending the data.
    /// </summary>
    public SctpStreamParameters? SctpStreamParameters { get; set; }

    /// <summary>
    /// A label which can be used to distinguish this DataChannel from others.
    /// </summary>
    public string? Label { get; set; }

    /// <summary>
    /// Name of the sub-protocol used by this DataChannel.
    /// </summary>
    public string? Protocol { get; set; }

    /// <summary>
    /// Custom application data.
    /// </summary>
    public TDataProducerAppData? AppData { get; set; }
}