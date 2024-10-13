using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace MediasoupSharp.Constants;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum WorkerLogTag
{
    /// <summary>
    /// Logs about software/library versions, configuration and process information.
    /// </summary>
    [EnumMember(Value = nameof(info))]
    info,

    /// <summary>
    /// Logs about ICE.
    /// </summary>
    [EnumMember(Value = nameof(ice))]
    ice,

    /// <summary>
    /// Logs about DTLS.
    /// </summary>
    [EnumMember(Value = nameof(dtls))]
    dtls,

    /// <summary>
    /// Logs about RTP.
    /// </summary>
    [EnumMember(Value = nameof(rtp))]
    rtp,

    /// <summary>
    /// Logs about SRTP encryption/decryption.
    /// </summary>
    [EnumMember(Value = nameof(srtp))]
    srtp,

    /// <summary>
    /// Logs about RTCP.
    /// </summary>
    [EnumMember(Value = nameof(rtcp))]
    rtcp,

    /// <summary>
    /// Logs about RTP retransmission, including NACK/PLI/FIR.
    /// </summary>
    [EnumMember(Value = nameof(rtx))]
    rtx,

    /// <summary>
    /// Logs about transport bandwidth estimation.
    /// </summary>
    [EnumMember(Value = nameof(bwe))]
    bwe,

    /// <summary>
    /// Logs related to the scores of Producers and Consumers.
    /// </summary>
    [EnumMember(Value = nameof(score))]
    score,

    /// <summary>
    /// Logs about video simulcast.
    /// </summary>
    [EnumMember(Value = nameof(simulcast))]
    simulcast,

    /// <summary>
    /// Logs about video SVC.
    /// </summary>
    [EnumMember(Value = nameof(svc))]
    svc,

    /// <summary>
    /// Logs about SCTP (DataChannel).
    /// </summary>
    [EnumMember(Value = nameof(sctp))]
    sctp,

    /// <summary>
    /// Logs about messages (can be SCTP messages or direct messages).
    /// </summary>
    [EnumMember(Value = nameof(message))]
    message,
}
