// <auto-generated>
//  automatically generated by the FlatBuffers compiler, do not modify
// </auto-generated>

using System;
using System.Collections.Generic;
using Google.FlatBuffers;
using System.Text.Json.Serialization;
using FlatBuffers.DataProducer;
using FlatBuffers.SctpParameters;
using MediasoupSharp.FlatBuffers.DataConsumer.T;
using Type = FlatBuffers.DataProducer.Type;

namespace FlatBuffers.DataConsumer
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
        public string DataProducerId { get { int o = __p.__offset(6); return o != 0 ? __p.__string(o + __p.bb_pos) : null; } }
#if ENABLE_SPAN_T
  public Span<byte> GetDataProducerIdBytes() { return __p.__vector_as_span<byte>(6, 1); }
#else
        public ArraySegment<byte>? GetDataProducerIdBytes() { return __p.__vector_as_arraysegment(6); }
#endif
        public byte[] GetDataProducerIdArray() { return __p.__vector_as_array<byte>(6); }
        public Type Type { get { int o = __p.__offset(8); return o != 0 ? (Type)__p.bb.Get(o + __p.bb_pos) : Type.SCTP; } }
        public SctpStreamParameters? SctpStreamParameters { get { int o = __p.__offset(10); return o != 0 ? (SctpStreamParameters?)(new SctpStreamParameters()).__assign(__p.__indirect(o + __p.bb_pos), __p.bb) : null; } }
        public string Label { get { int o = __p.__offset(12); return o != 0 ? __p.__string(o + __p.bb_pos) : null; } }
#if ENABLE_SPAN_T
  public Span<byte> GetLabelBytes() { return __p.__vector_as_span<byte>(12, 1); }
#else
        public ArraySegment<byte>? GetLabelBytes() { return __p.__vector_as_arraysegment(12); }
#endif
        public byte[] GetLabelArray() { return __p.__vector_as_array<byte>(12); }
        public string Protocol { get { int o = __p.__offset(14); return o != 0 ? __p.__string(o + __p.bb_pos) : null; } }
#if ENABLE_SPAN_T
  public Span<byte> GetProtocolBytes() { return __p.__vector_as_span<byte>(14, 1); }
#else
        public ArraySegment<byte>? GetProtocolBytes() { return __p.__vector_as_arraysegment(14); }
#endif
        public byte[] GetProtocolArray() { return __p.__vector_as_array<byte>(14); }
        public uint BufferedAmountLowThreshold { get { int o = __p.__offset(16); return o != 0 ? __p.bb.GetUint(o + __p.bb_pos) : (uint)0; } }
        public bool Paused { get { int o = __p.__offset(18); return o != 0 ? 0 != __p.bb.Get(o + __p.bb_pos) : (bool)false; } }
        public bool DataProducerPaused { get { int o = __p.__offset(20); return o != 0 ? 0 != __p.bb.Get(o + __p.bb_pos) : (bool)false; } }
        public ushort Subchannels(int j) { int o = __p.__offset(22); return o != 0 ? __p.bb.GetUshort(__p.__vector(o) + j * 2) : (ushort)0; }
        public int SubchannelsLength { get { int o = __p.__offset(22); return o != 0 ? __p.__vector_len(o) : 0; } }
#if ENABLE_SPAN_T
  public Span<ushort> GetSubchannelsBytes() { return __p.__vector_as_span<ushort>(22, 2); }
#else
        public ArraySegment<byte>? GetSubchannelsBytes() { return __p.__vector_as_arraysegment(22); }
#endif
        public ushort[] GetSubchannelsArray() { return __p.__vector_as_array<ushort>(22); }

        public static Offset<DumpResponse> CreateDumpResponse(FlatBufferBuilder builder,
                                                              StringOffset idOffset = default(StringOffset),
                                                              StringOffset data_producer_idOffset = default(StringOffset),
                                                              Type type = Type.SCTP,
                                                              Offset<SctpStreamParameters> sctp_stream_parametersOffset = default(Offset<SctpStreamParameters>),
                                                              StringOffset labelOffset = default(StringOffset),
                                                              StringOffset protocolOffset = default(StringOffset),
                                                              uint buffered_amount_low_threshold = 0,
                                                              bool paused = false,
                                                              bool data_producer_paused = false,
                                                              VectorOffset subchannelsOffset = default(VectorOffset))
        {
            builder.StartTable(10);
            DumpResponse.AddSubchannels(builder, subchannelsOffset);
            DumpResponse.AddBufferedAmountLowThreshold(builder, buffered_amount_low_threshold);
            DumpResponse.AddProtocol(builder, protocolOffset);
            DumpResponse.AddLabel(builder, labelOffset);
            DumpResponse.AddSctpStreamParameters(builder, sctp_stream_parametersOffset);
            DumpResponse.AddDataProducerId(builder, data_producer_idOffset);
            DumpResponse.AddId(builder, idOffset);
            DumpResponse.AddDataProducerPaused(builder, data_producer_paused);
            DumpResponse.AddPaused(builder, paused);
            DumpResponse.AddType(builder, type);
            return DumpResponse.EndDumpResponse(builder);
        }

        public static void StartDumpResponse(FlatBufferBuilder builder) { builder.StartTable(10); }
        public static void AddId(FlatBufferBuilder builder, StringOffset idOffset) { builder.AddOffset(0, idOffset.Value, 0); }
        public static void AddDataProducerId(FlatBufferBuilder builder, StringOffset dataProducerIdOffset) { builder.AddOffset(1, dataProducerIdOffset.Value, 0); }
        public static void AddType(FlatBufferBuilder builder, Type type) { builder.AddByte(2, (byte)type, 0); }
        public static void AddSctpStreamParameters(FlatBufferBuilder builder, Offset<SctpStreamParameters> sctpStreamParametersOffset) { builder.AddOffset(3, sctpStreamParametersOffset.Value, 0); }
        public static void AddLabel(FlatBufferBuilder builder, StringOffset labelOffset) { builder.AddOffset(4, labelOffset.Value, 0); }
        public static void AddProtocol(FlatBufferBuilder builder, StringOffset protocolOffset) { builder.AddOffset(5, protocolOffset.Value, 0); }
        public static void AddBufferedAmountLowThreshold(FlatBufferBuilder builder, uint bufferedAmountLowThreshold) { builder.AddUint(6, bufferedAmountLowThreshold, 0); }
        public static void AddPaused(FlatBufferBuilder builder, bool paused) { builder.AddBool(7, paused, false); }
        public static void AddDataProducerPaused(FlatBufferBuilder builder, bool dataProducerPaused) { builder.AddBool(8, dataProducerPaused, false); }
        public static void AddSubchannels(FlatBufferBuilder builder, VectorOffset subchannelsOffset) { builder.AddOffset(9, subchannelsOffset.Value, 0); }
        public static VectorOffset CreateSubchannelsVector(FlatBufferBuilder builder, ushort[] data) { builder.StartVector(2, data.Length, 2); for(int i = data.Length - 1; i >= 0; i--) builder.AddUshort(data[i]); return builder.EndVector(); }
        public static VectorOffset CreateSubchannelsVectorBlock(FlatBufferBuilder builder, ushort[] data) { builder.StartVector(2, data.Length, 2); builder.Add(data); return builder.EndVector(); }
        public static VectorOffset CreateSubchannelsVectorBlock(FlatBufferBuilder builder, ArraySegment<ushort> data) { builder.StartVector(2, data.Count, 2); builder.Add(data); return builder.EndVector(); }
        public static VectorOffset CreateSubchannelsVectorBlock(FlatBufferBuilder builder, IntPtr dataPtr, int sizeInBytes) { builder.StartVector(1, sizeInBytes, 1); builder.Add<ushort>(dataPtr, sizeInBytes); return builder.EndVector(); }
        public static void StartSubchannelsVector(FlatBufferBuilder builder, int numElems) { builder.StartVector(2, numElems, 2); }
        public static Offset<DumpResponse> EndDumpResponse(FlatBufferBuilder builder)
        {
            int o = builder.EndTable();
            builder.Required(o, 4);  // id
            builder.Required(o, 6);  // data_producer_id
            builder.Required(o, 12);  // label
            builder.Required(o, 14);  // protocol
            builder.Required(o, 22);  // subchannels
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
            _o.DataProducerId = this.DataProducerId;
            _o.Type = this.Type;
            _o.SctpStreamParameters = this.SctpStreamParameters.HasValue ? this.SctpStreamParameters.Value.UnPack() : null;
            _o.Label = this.Label;
            _o.Protocol = this.Protocol;
            _o.BufferedAmountLowThreshold = this.BufferedAmountLowThreshold;
            _o.Paused = this.Paused;
            _o.DataProducerPaused = this.DataProducerPaused;
            _o.Subchannels = new List<ushort>();
            for(var _j = 0; _j < this.SubchannelsLength; ++_j)
            { _o.Subchannels.Add(this.Subchannels(_j)); }
        }
        public static Offset<DumpResponse> Pack(FlatBufferBuilder builder, DumpResponseT _o)
        {
            if(_o == null)
                return default(Offset<DumpResponse>);
            var _id = _o.Id == null ? default(StringOffset) : builder.CreateString(_o.Id);
            var _data_producer_id = _o.DataProducerId == null ? default(StringOffset) : builder.CreateString(_o.DataProducerId);
            var _sctp_stream_parameters = _o.SctpStreamParameters == null ? default(Offset<SctpStreamParameters>) : SctpParameters.SctpStreamParameters.Pack(builder, _o.SctpStreamParameters);
            var _label = _o.Label == null ? default(StringOffset) : builder.CreateString(_o.Label);
            var _protocol = _o.Protocol == null ? default(StringOffset) : builder.CreateString(_o.Protocol);
            var _subchannels = default(VectorOffset);
            if(_o.Subchannels != null)
            {
                var __subchannels = _o.Subchannels.ToArray();
                _subchannels = CreateSubchannelsVector(builder, __subchannels);
            }
            return CreateDumpResponse(
              builder,
              _id,
              _data_producer_id,
              _o.Type,
              _sctp_stream_parameters,
              _label,
              _protocol,
              _o.BufferedAmountLowThreshold,
              _o.Paused,
              _o.DataProducerPaused,
              _subchannels);
        }
    }
}
