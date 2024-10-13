// <auto-generated>
//  automatically generated by the FlatBuffers compiler, do not modify
// </auto-generated>

using System.Text.Json.Serialization;
using Google.FlatBuffers;

namespace FlatBuffers.Transport
{
    public struct SetMaxIncomingBitrateRequest : IFlatbufferObject
    {
        private Table __p;
        public ByteBuffer ByteBuffer { get { return __p.bb; } }
        public static void ValidateVersion() { FlatBufferConstants.FLATBUFFERS_23_5_26(); }
        public static SetMaxIncomingBitrateRequest GetRootAsSetMaxIncomingBitrateRequest(ByteBuffer _bb) { return GetRootAsSetMaxIncomingBitrateRequest(_bb, new SetMaxIncomingBitrateRequest()); }
        public static SetMaxIncomingBitrateRequest GetRootAsSetMaxIncomingBitrateRequest(ByteBuffer _bb, SetMaxIncomingBitrateRequest obj) { return (obj.__assign(_bb.GetInt(_bb.Position) + _bb.Position, _bb)); }
        public void __init(int _i, ByteBuffer _bb) { __p = new Table(_i, _bb); }
        public SetMaxIncomingBitrateRequest __assign(int _i, ByteBuffer _bb) { __init(_i, _bb); return this; }

        public uint MaxIncomingBitrate { get { int o = __p.__offset(4); return o != 0 ? __p.bb.GetUint(o + __p.bb_pos) : (uint)0; } }

        public static Offset<SetMaxIncomingBitrateRequest> CreateSetMaxIncomingBitrateRequest(FlatBufferBuilder builder,
                                                                                              uint max_incoming_bitrate = 0)
        {
            builder.StartTable(1);
            SetMaxIncomingBitrateRequest.AddMaxIncomingBitrate(builder, max_incoming_bitrate);
            return SetMaxIncomingBitrateRequest.EndSetMaxIncomingBitrateRequest(builder);
        }

        public static void StartSetMaxIncomingBitrateRequest(FlatBufferBuilder builder) { builder.StartTable(1); }
        public static void AddMaxIncomingBitrate(FlatBufferBuilder builder, uint maxIncomingBitrate) { builder.AddUint(0, maxIncomingBitrate, 0); }
        public static Offset<SetMaxIncomingBitrateRequest> EndSetMaxIncomingBitrateRequest(FlatBufferBuilder builder)
        {
            int o = builder.EndTable();
            return new Offset<SetMaxIncomingBitrateRequest>(o);
        }
        public SetMaxIncomingBitrateRequestT UnPack()
        {
            var _o = new SetMaxIncomingBitrateRequestT();
            this.UnPackTo(_o);
            return _o;
        }
        public void UnPackTo(SetMaxIncomingBitrateRequestT _o)
        {
            _o.MaxIncomingBitrate = this.MaxIncomingBitrate;
        }
        public static Offset<SetMaxIncomingBitrateRequest> Pack(FlatBufferBuilder builder, SetMaxIncomingBitrateRequestT _o)
        {
            if(_o == null)
                return default(Offset<SetMaxIncomingBitrateRequest>);
            return CreateSetMaxIncomingBitrateRequest(
              builder,
              _o.MaxIncomingBitrate);
        }
    }

    public class SetMaxIncomingBitrateRequestT
    {
        [JsonPropertyName("max_incoming_bitrate")]
        public uint MaxIncomingBitrate { get; set; }

        public SetMaxIncomingBitrateRequestT()
        {
            this.MaxIncomingBitrate = 0;
        }
    }
}
