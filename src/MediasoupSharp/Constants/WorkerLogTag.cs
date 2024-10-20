using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace MediasoupSharp.Constants;

public enum WorkerLogTag
{
    /// <summary>
    /// Logs about software/library versions, configuration and process information.
    /// </summary>
    Info,

    /// <summary>
    /// Logs about ICE.
    /// </summary>
    Ice,

    /// <summary>
    /// Logs about DTLS.
    /// </summary>
    Dtls,

    /// <summary>
    /// Logs about RTP.
    /// </summary>
    Rtp,

    /// <summary>
    /// Logs about SRTP encryption/decryption.
    /// </summary>
    Srtp,

    /// <summary>
    /// Logs about RTCP.
    /// </summary>
    Rtcp,

    /// <summary>
    /// Logs about RTP retransmission, including NACK/PLI/FIR.
    /// </summary>
    Rtx,

    /// <summary>
    /// Logs about transport bandwidth estimation.
    /// </summary>
    Bwe,

    /// <summary>
    /// Logs related to the scores of Producers and Consumers.
    /// </summary>
    Score,

    /// <summary>
    /// Logs about video simulcast.
    /// </summary>
    Simulcast,

    /// <summary>
    /// Logs about video SVC.
    /// </summary>
    Svc,

    /// <summary>
    /// Logs about SCTP (DataChannel).
    /// </summary>
    Sctp,

    /// <summary>
    /// Logs about messages (can be SCTP messages or direct messages).
    /// </summary>
    Message,
}