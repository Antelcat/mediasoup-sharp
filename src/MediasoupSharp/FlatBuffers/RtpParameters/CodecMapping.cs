// <auto-generated>
//  automatically generated by the FlatBuffers compiler, do not modify
// </auto-generated>

using Google.FlatBuffers;
using MediasoupSharp.FlatBuffers.RtpParameters.T;

namespace FlatBuffers.RtpParameters
{
    public struct CodecMapping : IFlatbufferObject
    {
        private Table __p;
        public ByteBuffer ByteBuffer { get { return __p.bb; } }
        public static void ValidateVersion() { FlatBufferConstants.FLATBUFFERS_23_5_26(); }
        public static CodecMapping GetRootAsCodecMapping(ByteBuffer _bb) { return GetRootAsCodecMapping(_bb, new CodecMapping()); }
        public static CodecMapping GetRootAsCodecMapping(ByteBuffer _bb, CodecMapping obj) { return (obj.__assign(_bb.GetInt(_bb.Position) + _bb.Position, _bb)); }
        public void __init(int _i, ByteBuffer _bb) { __p = new Table(_i, _bb); }
        public CodecMapping __assign(int _i, ByteBuffer _bb) { __init(_i, _bb); return this; }

        public byte PayloadType { get { int o = __p.__offset(4); return o != 0 ? __p.bb.Get(o + __p.bb_pos) : (byte)0; } }
        public byte MappedPayloadType { get { int o = __p.__offset(6); return o != 0 ? __p.bb.Get(o + __p.bb_pos) : (byte)0; } }

        public static Offset<CodecMapping> CreateCodecMapping(FlatBufferBuilder builder,
                                                              byte payload_type = 0,
                                                              byte mapped_payload_type = 0)
        {
            builder.StartTable(2);
            CodecMapping.AddMappedPayloadType(builder, mapped_payload_type);
            CodecMapping.AddPayloadType(builder, payload_type);
            return CodecMapping.EndCodecMapping(builder);
        }

        public static void StartCodecMapping(FlatBufferBuilder builder) { builder.StartTable(2); }
        public static void AddPayloadType(FlatBufferBuilder builder, byte payloadType) { builder.AddByte(0, payloadType, 0); }
        public static void AddMappedPayloadType(FlatBufferBuilder builder, byte mappedPayloadType) { builder.AddByte(1, mappedPayloadType, 0); }
        public static Offset<CodecMapping> EndCodecMapping(FlatBufferBuilder builder)
        {
            int o = builder.EndTable();
            return new Offset<CodecMapping>(o);
        }
        public CodecMappingT UnPack()
        {
            var _o = new CodecMappingT();
            this.UnPackTo(_o);
            return _o;
        }
        public void UnPackTo(CodecMappingT _o)
        {
            _o.PayloadType = this.PayloadType;
            _o.MappedPayloadType = this.MappedPayloadType;
        }
        public static Offset<CodecMapping> Pack(FlatBufferBuilder builder, CodecMappingT _o)
        {
            if(_o == null)
                return default(Offset<CodecMapping>);
            return CreateCodecMapping(
              builder,
              _o.PayloadType,
              _o.MappedPayloadType);
        }
    }
}
