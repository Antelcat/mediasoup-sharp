// <auto-generated>
//  automatically generated by the FlatBuffers compiler, do not modify
// </auto-generated>

using System;
using Google.FlatBuffers;
using MediasoupSharp.FlatBuffers.RtpStream.T;

namespace FlatBuffers.RtpStream
{
    public struct BitrateByLayer : IFlatbufferObject
    {
        private Table __p;
        public ByteBuffer ByteBuffer { get { return __p.bb; } }
        public static void ValidateVersion() { FlatBufferConstants.FLATBUFFERS_23_5_26(); }
        public static BitrateByLayer GetRootAsBitrateByLayer(ByteBuffer _bb) { return GetRootAsBitrateByLayer(_bb, new BitrateByLayer()); }
        public static BitrateByLayer GetRootAsBitrateByLayer(ByteBuffer _bb, BitrateByLayer obj) { return (obj.__assign(_bb.GetInt(_bb.Position) + _bb.Position, _bb)); }
        public void __init(int _i, ByteBuffer _bb) { __p = new Table(_i, _bb); }
        public BitrateByLayer __assign(int _i, ByteBuffer _bb) { __init(_i, _bb); return this; }

        public string Layer { get { int o = __p.__offset(4); return o != 0 ? __p.__string(o + __p.bb_pos) : null; } }
#if ENABLE_SPAN_T
  public Span<byte> GetLayerBytes() { return __p.__vector_as_span<byte>(4, 1); }
#else
        public ArraySegment<byte>? GetLayerBytes() { return __p.__vector_as_arraysegment(4); }
#endif
        public byte[] GetLayerArray() { return __p.__vector_as_array<byte>(4); }
        public uint Bitrate { get { int o = __p.__offset(6); return o != 0 ? __p.bb.GetUint(o + __p.bb_pos) : (uint)0; } }

        public static Offset<BitrateByLayer> CreateBitrateByLayer(FlatBufferBuilder builder,
                                                                  StringOffset layerOffset = default(StringOffset),
                                                                  uint bitrate = 0)
        {
            builder.StartTable(2);
            BitrateByLayer.AddBitrate(builder, bitrate);
            BitrateByLayer.AddLayer(builder, layerOffset);
            return BitrateByLayer.EndBitrateByLayer(builder);
        }

        public static void StartBitrateByLayer(FlatBufferBuilder builder) { builder.StartTable(2); }
        public static void AddLayer(FlatBufferBuilder builder, StringOffset layerOffset) { builder.AddOffset(0, layerOffset.Value, 0); }
        public static void AddBitrate(FlatBufferBuilder builder, uint bitrate) { builder.AddUint(1, bitrate, 0); }
        public static Offset<BitrateByLayer> EndBitrateByLayer(FlatBufferBuilder builder)
        {
            int o = builder.EndTable();
            builder.Required(o, 4);  // layer
            return new Offset<BitrateByLayer>(o);
        }
        public BitrateByLayerT UnPack()
        {
            var _o = new BitrateByLayerT();
            this.UnPackTo(_o);
            return _o;
        }
        public void UnPackTo(BitrateByLayerT _o)
        {
            _o.Layer = this.Layer;
            _o.Bitrate = this.Bitrate;
        }
        public static Offset<BitrateByLayer> Pack(FlatBufferBuilder builder, BitrateByLayerT _o)
        {
            if(_o == null)
                return default(Offset<BitrateByLayer>);
            var _layer = _o.Layer == null ? default(StringOffset) : builder.CreateString(_o.Layer);
            return CreateBitrateByLayer(
              builder,
              _layer,
              _o.Bitrate);
        }
    }
}
