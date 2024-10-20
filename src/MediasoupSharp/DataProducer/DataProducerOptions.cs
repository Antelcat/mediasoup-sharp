﻿using FBS.SctpParameters;

namespace MediasoupSharp.DataProducer;

public class DataProducerOptions
{
    /// <summary>
    /// DataProducer id (just for Router.pipeToRouter() method).
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// SCTP parameters defining how the endpoint is sending the data.
    /// </summary>
    public SctpStreamParametersT? SctpStreamParameters { get; set; }

    /// <summary>
    /// A label which can be used to distinguish this DataChannel from others.
    /// </summary>
    public string? Label { get; set; }

    /// <summary>
    /// Name of the sub-protocol used by this DataChannel.
    /// </summary>
    public string? Protocol { get; set; }

    /// <summary>
    /// Whether the data producer must start in paused mode. Default false.
    /// </summary>
    public bool Paused { get; set; }

    /// <summary>
    /// Custom application data.
    /// </summary>
    public Dictionary<string, object>? AppData { get; set; }
}