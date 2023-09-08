﻿namespace MediasoupSharp.RtpParameters;

/// <summary>
/// The RTP send parameters describe a media stream received by mediasoup from
/// an endpoint through its corresponding mediasoup Producer. These parameters
/// may include a mid value that the mediasoup transport will use to match
/// received RTP packets based on their MID RTP extension value.
/// mediasoup allows RTP send parameters with a single encoding and with multiple
/// encodings (simulcast). In the latter case, each entry in the encodings array
/// must include a ssrc field or a rid field (the RID RTP extension value). Check
/// the Simulcast and SVC sections for more information.
/// The RTP receive parameters describe a media stream as sent by mediasoup to
/// an endpoint through its corresponding mediasoup Consumer. The mid value is
/// unset (mediasoup does not include the MID RTP extension into RTP packets
/// being sent to endpoints).
/// There is a single entry in the encodings array (even if the corresponding
/// producer uses simulcast). The consumer sends a single and continuous RTP
/// stream to the endpoint and spatial/temporal layer selection is possible via
/// consumer.setPreferredLayers().
/// As an exception, previous bullet is not true when consuming a stream over a
/// PipeTransport, in which all RTP streams from the associated producer are
/// forwarded verbatim through the consumer.
/// The RTP receive parameters will always have their ssrc values randomly
/// generated for all of its  encodings (and optional rtx: { ssrc: XXXX } if the
/// endpoint supports RTX), regardless of the original RTP send parameters in
/// the associated producer. This applies even if the producer's encodings have
/// rid set.
/// </summary>
public record RtcpParameters
{
    /// <summary>
    /// The Canonical Name (CNAME) used by RTCP (e.g. in SDES messages).
    /// </summary>
    public string? Cname { get; set; }

    /// <summary>
    /// Whether reduced size RTCP RFC 5506 is configured (if true) or compound RTCP
    /// as specified in RFC 3550 (if false). Default true.
    /// </summary>
    public bool? ReducedSize { get; set; }

    /// <summary>
    /// Whether RTCP-mux is used. Default true.
    /// </summary>
    public bool? Mux { get; set; }
}