// <auto-generated>
//  automatically generated by the FlatBuffers compiler, do not modify
// </auto-generated>

using System.Text.Json.Serialization;
using Google.FlatBuffers;
using MediasoupSharp.FlatBuffers.Consumer.T;

namespace FlatBuffers.Consumer
{
    public struct PliTraceInfo : IFlatbufferObject
    {
        private Table __p;
        public ByteBuffer ByteBuffer { get { return __p.bb; } }
        public static void ValidateVersion() { FlatBufferConstants.FLATBUFFERS_23_5_26(); }
        public static PliTraceInfo GetRootAsPliTraceInfo(ByteBuffer _bb) { return GetRootAsPliTraceInfo(_bb, new PliTraceInfo()); }
        public static PliTraceInfo GetRootAsPliTraceInfo(ByteBuffer _bb, PliTraceInfo obj) { return (obj.__assign(_bb.GetInt(_bb.Position) + _bb.Position, _bb)); }
        public void __init(int _i, ByteBuffer _bb) { __p = new Table(_i, _bb); }
        public PliTraceInfo __assign(int _i, ByteBuffer _bb) { __init(_i, _bb); return this; }

        public uint Ssrc { get { int o = __p.__offset(4); return o != 0 ? __p.bb.GetUint(o + __p.bb_pos) : (uint)0; } }

        public static Offset<PliTraceInfo> CreatePliTraceInfo(FlatBufferBuilder builder,
                                                              uint ssrc = 0)
        {
            builder.StartTable(1);
            PliTraceInfo.AddSsrc(builder, ssrc);
            return PliTraceInfo.EndPliTraceInfo(builder);
        }

        public static void StartPliTraceInfo(FlatBufferBuilder builder) { builder.StartTable(1); }
        public static void AddSsrc(FlatBufferBuilder builder, uint ssrc) { builder.AddUint(0, ssrc, 0); }
        public static Offset<PliTraceInfo> EndPliTraceInfo(FlatBufferBuilder builder)
        {
            int o = builder.EndTable();
            return new Offset<PliTraceInfo>(o);
        }
        public PliTraceInfoT UnPack()
        {
            var _o = new PliTraceInfoT();
            this.UnPackTo(_o);
            return _o;
        }
        public void UnPackTo(PliTraceInfoT _o)
        {
            _o.Ssrc = this.Ssrc;
        }
        public static Offset<PliTraceInfo> Pack(FlatBufferBuilder builder, PliTraceInfoT _o)
        {
            if(_o == null)
                return default(Offset<PliTraceInfo>);
            return CreatePliTraceInfo(
              builder,
              _o.Ssrc);
        }
    }
}
