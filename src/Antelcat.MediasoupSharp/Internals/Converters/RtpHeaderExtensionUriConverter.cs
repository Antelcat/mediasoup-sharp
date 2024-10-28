using FBS.RtpParameters;

namespace Antelcat.MediasoupSharp.Internals.Converters;

internal class RtpHeaderExtensionUriConverter : EnumStringConverter<RtpHeaderExtensionUri>
{
    protected override IEnumerable<(RtpHeaderExtensionUri Enum, string Text)> Map()
    {
        yield return (RtpHeaderExtensionUri.Mid,"urn:ietf:params:rtp-hdrext:sdes:mid");
        yield return (RtpHeaderExtensionUri.RtpStreamId,"urn:ietf:params:rtp-hdrext:sdes:rtp-stream-id");
        yield return (RtpHeaderExtensionUri.RepairRtpStreamId,"urn:ietf:params:rtp-hdrext:sdes:repaired-rtp-stream-id");
        yield return (RtpHeaderExtensionUri.FrameMarkingDraft07,"http://tools.ietf.org/html/draft-ietf-avtext-framemarking-07");
        yield return (RtpHeaderExtensionUri.FrameMarking,"urn:ietf:params:rtp-hdrext:framemarking");
        yield return (RtpHeaderExtensionUri.AudioLevel,"urn:ietf:params:rtp-hdrext:ssrc-audio-level");
        yield return (RtpHeaderExtensionUri.VideoOrientation,"urn:3gpp:video-orientation");
        yield return (RtpHeaderExtensionUri.TimeOffset,"urn:ietf:params:rtp-hdrext:toffset");
        yield return (RtpHeaderExtensionUri.TransportWideCcDraft01,"http://www.ietf.org/id/draft-holmer-rmcat-transport-wide-cc-extensions-01");
        yield return (RtpHeaderExtensionUri.AbsSendTime,"http://www.webrtc.org/experiments/rtp-hdrext/abs-send-time");
        yield return (RtpHeaderExtensionUri.AbsCaptureTime,"http://www.webrtc.org/experiments/rtp-hdrext/abs-capture-time");
        yield return (RtpHeaderExtensionUri.PlayoutDelay,"http://www.webrtc.org/experiments/rtp-hdrext/playout-delay");
    }
}