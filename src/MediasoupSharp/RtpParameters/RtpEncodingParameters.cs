﻿namespace MediasoupSharp.RtpParameters;

public record RtpEncodingParameters
{
    /// <summary>
    /// The media SSRC.
    /// </summary>
    public int? Ssrc { get; set; }

    /// <summary>
    /// The RID RTP extension value. Must be unique.
    /// </summary>
    public string? Rid { get; set; }

    /// <summary>
    /// Codec payload type this encoding affects. If unset, first media codec is
    /// chosen.
    /// </summary>
    public int? CodecPayloadType { get; set; }

    /// <summary>
    /// RTX stream information. It must contain a numeric ssrc field indicating
    /// the RTX SSRC.
    /// { Ssrc : int }
    /// </summary>
    public RTX? Rtx { get; set; }
    public record RTX(int Ssrc);

    /// <summary>
    /// It indicates whether discontinuous RTP transmission will be used. Useful
    /// for audio (if the codec supports it) and for video screen sharing (when
    /// static content is being transmitted, this option disables the RTP
    /// inactivity checks in mediasoup). Default false.
    /// </summary>
    public bool? Dtx { get; set; }

    /// <summary>
    /// Number of spatial and temporal layers in the RTP stream (e.g. 'L1T3').
    /// See webrtc-svc.
    /// </summary>
    public string? ScalabilityMode { get; set; }

    /// <summary>
    /// Others.
    /// </summary>
    public int? ScaleResolutionDownBy { get; set; }

    public int? MaxBitrate { get; set; }
}