using MediasoupSharp.FlatBuffers.Consumer.T;

namespace MediasoupSharp.FlatBuffers.Consumer;

public enum TraceInfo : byte
{
    NONE = 0,

    KeyFrameTraceInfo = 1,

    FirTraceInfo = 2,

    PliTraceInfo = 3,

    RtpTraceInfo = 4,
}

public class TraceInfoUnion
{
    public TraceInfo Type  { get; set; }
    public object    Value { get; set; }

    public T As<T>() where T : class { return (this.Value as T)!; }
    public KeyFrameTraceInfoT AsKeyFrameTraceInfo() { return this.As<KeyFrameTraceInfoT>(); }
    public static TraceInfoUnion FromKeyFrameTraceInfo(KeyFrameTraceInfoT _keyframetraceinfo) { return new TraceInfoUnion { Type = TraceInfo.KeyFrameTraceInfo, Value = _keyframetraceinfo }; }
    public FirTraceInfoT AsFirTraceInfo() { return this.As<FirTraceInfoT>(); }
    public static TraceInfoUnion FromFirTraceInfo(FirTraceInfoT _firtraceinfo) { return new TraceInfoUnion { Type = TraceInfo.FirTraceInfo, Value = _firtraceinfo }; }
    public PliTraceInfoT AsPliTraceInfo() { return this.As<PliTraceInfoT>(); }
    public static TraceInfoUnion FromPliTraceInfo(PliTraceInfoT _plitraceinfo) { return new TraceInfoUnion { Type = TraceInfo.PliTraceInfo, Value = _plitraceinfo }; }
    public RtpTraceInfoT AsRtpTraceInfo() { return this.As<RtpTraceInfoT>(); }
    public static TraceInfoUnion FromRtpTraceInfo(RtpTraceInfoT _rtptraceinfo) { return new TraceInfoUnion { Type = TraceInfo.RtpTraceInfo, Value = _rtptraceinfo }; }

    public static int Pack(Google.FlatBuffers.FlatBufferBuilder builder, TraceInfoUnion _o)
    {
        switch(_o.Type)
        {
            default:
                return 0;
            case TraceInfo.KeyFrameTraceInfo:
                return global::FlatBuffers.Consumer.KeyFrameTraceInfo.Pack(builder, _o.AsKeyFrameTraceInfo()).Value;
            case TraceInfo.FirTraceInfo:
                return global::FlatBuffers.Consumer.FirTraceInfo.Pack(builder, _o.AsFirTraceInfo()).Value;
            case TraceInfo.PliTraceInfo:
                return global::FlatBuffers.Consumer.PliTraceInfo.Pack(builder, _o.AsPliTraceInfo()).Value;
            case TraceInfo.RtpTraceInfo:
                return global::FlatBuffers.Consumer.RtpTraceInfo.Pack(builder, _o.AsRtpTraceInfo()).Value;
        }
    }
}
