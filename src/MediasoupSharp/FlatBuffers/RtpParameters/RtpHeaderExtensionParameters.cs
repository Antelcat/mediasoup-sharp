// <auto-generated>
//  automatically generated by the FlatBuffers compiler, do not modify
// </auto-generated>

using System;
using System.Collections.Generic;
using Google.FlatBuffers;
using System.Text.Json.Serialization;
using MediasoupSharp.FlatBuffers.RtpParameters.T;

namespace FlatBuffers.RtpParameters
{
    public struct RtpHeaderExtensionParameters : IFlatbufferObject
    {
        private Table __p;
        public ByteBuffer ByteBuffer { get { return __p.bb; } }
        public static void ValidateVersion() { FlatBufferConstants.FLATBUFFERS_23_5_26(); }
        public static RtpHeaderExtensionParameters GetRootAsRtpHeaderExtensionParameters(ByteBuffer _bb) { return GetRootAsRtpHeaderExtensionParameters(_bb, new RtpHeaderExtensionParameters()); }
        public static RtpHeaderExtensionParameters GetRootAsRtpHeaderExtensionParameters(ByteBuffer _bb, RtpHeaderExtensionParameters obj) { return (obj.__assign(_bb.GetInt(_bb.Position) + _bb.Position, _bb)); }
        public void __init(int _i, ByteBuffer _bb) { __p = new Table(_i, _bb); }
        public RtpHeaderExtensionParameters __assign(int _i, ByteBuffer _bb) { __init(_i, _bb); return this; }

        public RtpHeaderExtensionUri Uri { get { int o = __p.__offset(4); return o != 0 ? (RtpHeaderExtensionUri)__p.bb.Get(o + __p.bb_pos) : RtpHeaderExtensionUri.Mid; } }
        public byte Id { get { int o = __p.__offset(6); return o != 0 ? __p.bb.Get(o + __p.bb_pos) : (byte)0; } }
        public bool Encrypt { get { int o = __p.__offset(8); return o != 0 ? 0 != __p.bb.Get(o + __p.bb_pos) : (bool)false; } }
        public Parameter? Parameters(int j) { int o = __p.__offset(10); return o != 0 ? (Parameter?)(new Parameter()).__assign(__p.__indirect(__p.__vector(o) + j * 4), __p.bb) : null; }
        public int ParametersLength { get { int o = __p.__offset(10); return o != 0 ? __p.__vector_len(o) : 0; } }

        public static Offset<RtpHeaderExtensionParameters> CreateRtpHeaderExtensionParameters(FlatBufferBuilder builder,
                                                                                              RtpHeaderExtensionUri uri = RtpHeaderExtensionUri.Mid,
                                                                                              byte id = 0,
                                                                                              bool encrypt = false,
                                                                                              VectorOffset parametersOffset = default(VectorOffset))
        {
            builder.StartTable(4);
            RtpHeaderExtensionParameters.AddParameters(builder, parametersOffset);
            RtpHeaderExtensionParameters.AddEncrypt(builder, encrypt);
            RtpHeaderExtensionParameters.AddId(builder, id);
            RtpHeaderExtensionParameters.AddUri(builder, uri);
            return RtpHeaderExtensionParameters.EndRtpHeaderExtensionParameters(builder);
        }

        public static void StartRtpHeaderExtensionParameters(FlatBufferBuilder builder) { builder.StartTable(4); }
        public static void AddUri(FlatBufferBuilder builder, RtpHeaderExtensionUri uri) { builder.AddByte(0, (byte)uri, 0); }
        public static void AddId(FlatBufferBuilder builder, byte id) { builder.AddByte(1, id, 0); }
        public static void AddEncrypt(FlatBufferBuilder builder, bool encrypt) { builder.AddBool(2, encrypt, false); }
        public static void AddParameters(FlatBufferBuilder builder, VectorOffset parametersOffset) { builder.AddOffset(3, parametersOffset.Value, 0); }
        public static VectorOffset CreateParametersVector(FlatBufferBuilder builder, Offset<Parameter>[] data) { builder.StartVector(4, data.Length, 4); for(int i = data.Length - 1; i >= 0; i--) builder.AddOffset(data[i].Value); return builder.EndVector(); }
        public static VectorOffset CreateParametersVectorBlock(FlatBufferBuilder builder, Offset<Parameter>[] data) { builder.StartVector(4, data.Length, 4); builder.Add(data); return builder.EndVector(); }
        public static VectorOffset CreateParametersVectorBlock(FlatBufferBuilder builder, ArraySegment<Offset<Parameter>> data) { builder.StartVector(4, data.Count, 4); builder.Add(data); return builder.EndVector(); }
        public static VectorOffset CreateParametersVectorBlock(FlatBufferBuilder builder, IntPtr dataPtr, int sizeInBytes) { builder.StartVector(1, sizeInBytes, 1); builder.Add<Offset<Parameter>>(dataPtr, sizeInBytes); return builder.EndVector(); }
        public static void StartParametersVector(FlatBufferBuilder builder, int numElems) { builder.StartVector(4, numElems, 4); }
        public static Offset<RtpHeaderExtensionParameters> EndRtpHeaderExtensionParameters(FlatBufferBuilder builder)
        {
            int o = builder.EndTable();
            return new Offset<RtpHeaderExtensionParameters>(o);
        }
        public RtpHeaderExtensionParametersT UnPack()
        {
            var _o = new RtpHeaderExtensionParametersT();
            this.UnPackTo(_o);
            return _o;
        }
        public void UnPackTo(RtpHeaderExtensionParametersT _o)
        {
            _o.Uri = this.Uri;
            _o.Id = this.Id;
            _o.Encrypt = this.Encrypt;
            _o.Parameters = new List<ParameterT>();
            for(var _j = 0; _j < this.ParametersLength; ++_j)
            { _o.Parameters.Add(this.Parameters(_j).HasValue ? this.Parameters(_j).Value.UnPack() : null); }
        }
        public static Offset<RtpHeaderExtensionParameters> Pack(FlatBufferBuilder builder, RtpHeaderExtensionParametersT _o)
        {
            if(_o == null)
                return default(Offset<RtpHeaderExtensionParameters>);
            var _parameters = default(VectorOffset);
            if(_o.Parameters != null)
            {
                var __parameters = new Offset<Parameter>[_o.Parameters.Count];
                for(var _j = 0; _j < __parameters.Length; ++_j)
                { __parameters[_j] = Parameter.Pack(builder, _o.Parameters[_j]); }
                _parameters = CreateParametersVector(builder, __parameters);
            }
            return CreateRtpHeaderExtensionParameters(
              builder,
              _o.Uri,
              _o.Id,
              _o.Encrypt,
              _parameters);
        }
    }
}
