// <auto-generated>
//  automatically generated by the FlatBuffers compiler, do not modify
// </auto-generated>

using System;
using System.Collections.Generic;
using Google.FlatBuffers;
using System.Text.Json.Serialization;
using FlatBuffers.RtpStream;
using MediasoupSharp.FlatBuffers.Producer.T;

namespace FlatBuffers.Producer
{
    public struct GetStatsResponse : IFlatbufferObject
    {
        private Table __p;
        public ByteBuffer ByteBuffer { get { return __p.bb; } }
        public static void ValidateVersion() { FlatBufferConstants.FLATBUFFERS_23_5_26(); }
        public static GetStatsResponse GetRootAsGetStatsResponse(ByteBuffer _bb) { return GetRootAsGetStatsResponse(_bb, new GetStatsResponse()); }
        public static GetStatsResponse GetRootAsGetStatsResponse(ByteBuffer _bb, GetStatsResponse obj) { return (obj.__assign(_bb.GetInt(_bb.Position) + _bb.Position, _bb)); }
        public void __init(int _i, ByteBuffer _bb) { __p = new Table(_i, _bb); }
        public GetStatsResponse __assign(int _i, ByteBuffer _bb) { __init(_i, _bb); return this; }

        public Stats? Stats(int j) { int o = __p.__offset(4); return o != 0 ? (Stats?)(new Stats()).__assign(__p.__indirect(__p.__vector(o) + j * 4), __p.bb) : null; }
        public int StatsLength { get { int o = __p.__offset(4); return o != 0 ? __p.__vector_len(o) : 0; } }

        public static Offset<GetStatsResponse> CreateGetStatsResponse(FlatBufferBuilder builder,
                                                                      VectorOffset statsOffset = default(VectorOffset))
        {
            builder.StartTable(1);
            GetStatsResponse.AddStats(builder, statsOffset);
            return GetStatsResponse.EndGetStatsResponse(builder);
        }

        public static void StartGetStatsResponse(FlatBufferBuilder builder) { builder.StartTable(1); }
        public static void AddStats(FlatBufferBuilder builder, VectorOffset statsOffset) { builder.AddOffset(0, statsOffset.Value, 0); }
        public static VectorOffset CreateStatsVector(FlatBufferBuilder builder, Offset<Stats>[] data) { builder.StartVector(4, data.Length, 4); for(int i = data.Length - 1; i >= 0; i--) builder.AddOffset(data[i].Value); return builder.EndVector(); }
        public static VectorOffset CreateStatsVectorBlock(FlatBufferBuilder builder, Offset<Stats>[] data) { builder.StartVector(4, data.Length, 4); builder.Add(data); return builder.EndVector(); }
        public static VectorOffset CreateStatsVectorBlock(FlatBufferBuilder builder, ArraySegment<Offset<Stats>> data) { builder.StartVector(4, data.Count, 4); builder.Add(data); return builder.EndVector(); }
        public static VectorOffset CreateStatsVectorBlock(FlatBufferBuilder builder, IntPtr dataPtr, int sizeInBytes) { builder.StartVector(1, sizeInBytes, 1); builder.Add<Offset<Stats>>(dataPtr, sizeInBytes); return builder.EndVector(); }
        public static void StartStatsVector(FlatBufferBuilder builder, int numElems) { builder.StartVector(4, numElems, 4); }
        public static Offset<GetStatsResponse> EndGetStatsResponse(FlatBufferBuilder builder)
        {
            int o = builder.EndTable();
            builder.Required(o, 4);  // stats
            return new Offset<GetStatsResponse>(o);
        }
        public GetStatsResponseT UnPack()
        {
            var _o = new GetStatsResponseT();
            this.UnPackTo(_o);
            return _o;
        }
        public void UnPackTo(GetStatsResponseT _o)
        {
            _o.Stats = new List<StatsT>();
            for(var _j = 0; _j < this.StatsLength; ++_j)
            { _o.Stats.Add(this.Stats(_j).HasValue ? this.Stats(_j).Value.UnPack() : null); }
        }
        public static Offset<GetStatsResponse> Pack(FlatBufferBuilder builder, GetStatsResponseT _o)
        {
            if(_o == null)
                return default(Offset<GetStatsResponse>);
            var _stats = default(VectorOffset);
            if(_o.Stats != null)
            {
                var __stats = new Offset<Stats>[_o.Stats.Count];
                for(var _j = 0; _j < __stats.Length; ++_j)
                { __stats[_j] = RtpStream.Stats.Pack(builder, _o.Stats[_j]); }
                _stats = CreateStatsVector(builder, __stats);
            }
            return CreateGetStatsResponse(
              builder,
              _stats);
        }
    }
}
