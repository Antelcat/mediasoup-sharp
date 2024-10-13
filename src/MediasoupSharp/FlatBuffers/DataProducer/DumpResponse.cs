// <auto-generated>
//  automatically generated by the FlatBuffers compiler, do not modify
// </auto-generated>

using System;
using Google.FlatBuffers;
using System.Text.Json.Serialization;
using FlatBuffers.SctpParameters;
using MediasoupSharp.FlatBuffers.DataProducer.T;

namespace FlatBuffers.DataProducer
{
    public struct DumpResponse : IFlatbufferObject
    {
        private Table __p;
        public ByteBuffer ByteBuffer { get { return __p.bb; } }
        public static void ValidateVersion() { FlatBufferConstants.FLATBUFFERS_23_5_26(); }
        public static DumpResponse GetRootAsDumpResponse(ByteBuffer _bb) { return GetRootAsDumpResponse(_bb, new DumpResponse()); }
        public static DumpResponse GetRootAsDumpResponse(ByteBuffer _bb, DumpResponse obj) { return (obj.__assign(_bb.GetInt(_bb.Position) + _bb.Position, _bb)); }
        public void __init(int _i, ByteBuffer _bb) { __p = new Table(_i, _bb); }
        public DumpResponse __assign(int _i, ByteBuffer _bb) { __init(_i, _bb); return this; }

        public string Id { get { int o = __p.__offset(4); return o != 0 ? __p.__string(o + __p.bb_pos) : null; } }
#if ENABLE_SPAN_T
  public Span<byte> GetIdBytes() { return __p.__vector_as_span<byte>(4, 1); }
#else
        public ArraySegment<byte>? GetIdBytes() { return __p.__vector_as_arraysegment(4); }
#endif
        public byte[] GetIdArray() { return __p.__vector_as_array<byte>(4); }
        public Type Type { get { int o = __p.__offset(6); return o != 0 ? (Type)__p.bb.Get(o + __p.bb_pos) : Type.SCTP; } }
        public SctpStreamParameters? SctpStreamParameters { get { int o = __p.__offset(8); return o != 0 ? (SctpStreamParameters?)(new SctpStreamParameters()).__assign(__p.__indirect(o + __p.bb_pos), __p.bb) : null; } }
        public string Label { get { int o = __p.__offset(10); return o != 0 ? __p.__string(o + __p.bb_pos) : null; } }
#if ENABLE_SPAN_T
  public Span<byte> GetLabelBytes() { return __p.__vector_as_span<byte>(10, 1); }
#else
        public ArraySegment<byte>? GetLabelBytes() { return __p.__vector_as_arraysegment(10); }
#endif
        public byte[] GetLabelArray() { return __p.__vector_as_array<byte>(10); }
        public string Protocol { get { int o = __p.__offset(12); return o != 0 ? __p.__string(o + __p.bb_pos) : null; } }
#if ENABLE_SPAN_T
  public Span<byte> GetProtocolBytes() { return __p.__vector_as_span<byte>(12, 1); }
#else
        public ArraySegment<byte>? GetProtocolBytes() { return __p.__vector_as_arraysegment(12); }
#endif
        public byte[] GetProtocolArray() { return __p.__vector_as_array<byte>(12); }
        public bool Paused { get { int o = __p.__offset(14); return o != 0 ? 0 != __p.bb.Get(o + __p.bb_pos) : (bool)false; } }

        public static Offset<DumpResponse> CreateDumpResponse(FlatBufferBuilder builder,
                                                              StringOffset idOffset = default(StringOffset),
                                                              Type type = Type.SCTP,
                                                              Offset<SctpStreamParameters> sctp_stream_parametersOffset = default(Offset<SctpStreamParameters>),
                                                              StringOffset labelOffset = default(StringOffset),
                                                              StringOffset protocolOffset = default(StringOffset),
                                                              bool paused = false)
        {
            builder.StartTable(6);
            DumpResponse.AddProtocol(builder, protocolOffset);
            DumpResponse.AddLabel(builder, labelOffset);
            DumpResponse.AddSctpStreamParameters(builder, sctp_stream_parametersOffset);
            DumpResponse.AddId(builder, idOffset);
            DumpResponse.AddPaused(builder, paused);
            DumpResponse.AddType(builder, type);
            return DumpResponse.EndDumpResponse(builder);
        }

        public static void StartDumpResponse(FlatBufferBuilder builder) { builder.StartTable(6); }
        public static void AddId(FlatBufferBuilder builder, StringOffset idOffset) { builder.AddOffset(0, idOffset.Value, 0); }
        public static void AddType(FlatBufferBuilder builder, Type type) { builder.AddByte(1, (byte)type, 0); }
        public static void AddSctpStreamParameters(FlatBufferBuilder builder, Offset<SctpStreamParameters> sctpStreamParametersOffset) { builder.AddOffset(2, sctpStreamParametersOffset.Value, 0); }
        public static void AddLabel(FlatBufferBuilder builder, StringOffset labelOffset) { builder.AddOffset(3, labelOffset.Value, 0); }
        public static void AddProtocol(FlatBufferBuilder builder, StringOffset protocolOffset) { builder.AddOffset(4, protocolOffset.Value, 0); }
        public static void AddPaused(FlatBufferBuilder builder, bool paused) { builder.AddBool(5, paused, false); }
        public static Offset<DumpResponse> EndDumpResponse(FlatBufferBuilder builder)
        {
            int o = builder.EndTable();
            builder.Required(o, 4);  // id
            builder.Required(o, 10);  // label
            builder.Required(o, 12);  // protocol
            return new Offset<DumpResponse>(o);
        }
        public DumpResponseT UnPack()
        {
            var _o = new DumpResponseT();
            this.UnPackTo(_o);
            return _o;
        }
        public void UnPackTo(DumpResponseT _o)
        {
            _o.Id = this.Id;
            _o.Type = this.Type;
            _o.SctpStreamParameters = this.SctpStreamParameters.HasValue ? this.SctpStreamParameters.Value.UnPack() : null;
            _o.Label = this.Label;
            _o.Protocol = this.Protocol;
            _o.Paused = this.Paused;
        }
        public static Offset<DumpResponse> Pack(FlatBufferBuilder builder, DumpResponseT _o)
        {
            if(_o == null)
                return default(Offset<DumpResponse>);
            var _id = _o.Id == null ? default(StringOffset) : builder.CreateString(_o.Id);
            var _sctp_stream_parameters = _o.SctpStreamParameters == null ? default(Offset<SctpStreamParameters>) : SctpParameters.SctpStreamParameters.Pack(builder, _o.SctpStreamParameters);
            var _label = _o.Label == null ? default(StringOffset) : builder.CreateString(_o.Label);
            var _protocol = _o.Protocol == null ? default(StringOffset) : builder.CreateString(_o.Protocol);
            return CreateDumpResponse(
              builder,
              _id,
              _o.Type,
              _sctp_stream_parameters,
              _label,
              _protocol,
              _o.Paused);
        }
    }
}
