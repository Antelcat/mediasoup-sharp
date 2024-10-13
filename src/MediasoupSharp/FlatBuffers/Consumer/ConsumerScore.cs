// <auto-generated>
//  automatically generated by the FlatBuffers compiler, do not modify
// </auto-generated>

using System;
using System.Collections.Generic;
using Google.FlatBuffers;
using System.Text.Json.Serialization;
using MediasoupSharp.FlatBuffers.Consumer.T;

namespace FlatBuffers.Consumer
{
    public struct ConsumerScore : IFlatbufferObject
    {
        private Table __p;
        public ByteBuffer ByteBuffer { get { return __p.bb; } }
        public static void ValidateVersion() { FlatBufferConstants.FLATBUFFERS_23_5_26(); }
        public static ConsumerScore GetRootAsConsumerScore(ByteBuffer _bb) { return GetRootAsConsumerScore(_bb, new ConsumerScore()); }
        public static ConsumerScore GetRootAsConsumerScore(ByteBuffer _bb, ConsumerScore obj) { return (obj.__assign(_bb.GetInt(_bb.Position) + _bb.Position, _bb)); }
        public void __init(int _i, ByteBuffer _bb) { __p = new Table(_i, _bb); }
        public ConsumerScore __assign(int _i, ByteBuffer _bb) { __init(_i, _bb); return this; }

        public byte Score { get { int o = __p.__offset(4); return o != 0 ? __p.bb.Get(o + __p.bb_pos) : (byte)0; } }
        public byte ProducerScore { get { int o = __p.__offset(6); return o != 0 ? __p.bb.Get(o + __p.bb_pos) : (byte)0; } }
        public byte ProducerScores(int j) { int o = __p.__offset(8); return o != 0 ? __p.bb.Get(__p.__vector(o) + j * 1) : (byte)0; }
        public int ProducerScoresLength { get { int o = __p.__offset(8); return o != 0 ? __p.__vector_len(o) : 0; } }
#if ENABLE_SPAN_T
  public Span<byte> GetProducerScoresBytes() { return __p.__vector_as_span<byte>(8, 1); }
#else
        public ArraySegment<byte>? GetProducerScoresBytes() { return __p.__vector_as_arraysegment(8); }
#endif
        public byte[] GetProducerScoresArray() { return __p.__vector_as_array<byte>(8); }

        public static Offset<ConsumerScore> CreateConsumerScore(FlatBufferBuilder builder,
                                                                byte score = 0,
                                                                byte producer_score = 0,
                                                                VectorOffset producer_scoresOffset = default(VectorOffset))
        {
            builder.StartTable(3);
            ConsumerScore.AddProducerScores(builder, producer_scoresOffset);
            ConsumerScore.AddProducerScore(builder, producer_score);
            ConsumerScore.AddScore(builder, score);
            return ConsumerScore.EndConsumerScore(builder);
        }

        public static void StartConsumerScore(FlatBufferBuilder builder) { builder.StartTable(3); }
        public static void AddScore(FlatBufferBuilder builder, byte score) { builder.AddByte(0, score, 0); }
        public static void AddProducerScore(FlatBufferBuilder builder, byte producerScore) { builder.AddByte(1, producerScore, 0); }
        public static void AddProducerScores(FlatBufferBuilder builder, VectorOffset producerScoresOffset) { builder.AddOffset(2, producerScoresOffset.Value, 0); }
        public static VectorOffset CreateProducerScoresVector(FlatBufferBuilder builder, byte[] data) { builder.StartVector(1, data.Length, 1); for(int i = data.Length - 1; i >= 0; i--) builder.AddByte(data[i]); return builder.EndVector(); }
        public static VectorOffset CreateProducerScoresVectorBlock(FlatBufferBuilder builder, byte[] data) { builder.StartVector(1, data.Length, 1); builder.Add(data); return builder.EndVector(); }
        public static VectorOffset CreateProducerScoresVectorBlock(FlatBufferBuilder builder, ArraySegment<byte> data) { builder.StartVector(1, data.Count, 1); builder.Add(data); return builder.EndVector(); }
        public static VectorOffset CreateProducerScoresVectorBlock(FlatBufferBuilder builder, IntPtr dataPtr, int sizeInBytes) { builder.StartVector(1, sizeInBytes, 1); builder.Add<byte>(dataPtr, sizeInBytes); return builder.EndVector(); }
        public static void StartProducerScoresVector(FlatBufferBuilder builder, int numElems) { builder.StartVector(1, numElems, 1); }
        public static Offset<ConsumerScore> EndConsumerScore(FlatBufferBuilder builder)
        {
            int o = builder.EndTable();
            builder.Required(o, 8);  // producer_scores
            return new Offset<ConsumerScore>(o);
        }
        public ConsumerScoreT UnPack()
        {
            var _o = new ConsumerScoreT();
            this.UnPackTo(_o);
            return _o;
        }
        public void UnPackTo(ConsumerScoreT _o)
        {
            _o.Score = this.Score;
            _o.ProducerScore = this.ProducerScore;
            _o.ProducerScores = new List<byte>();
            for(var _j = 0; _j < this.ProducerScoresLength; ++_j)
            { _o.ProducerScores.Add(this.ProducerScores(_j)); }
        }
        public static Offset<ConsumerScore> Pack(FlatBufferBuilder builder, ConsumerScoreT _o)
        {
            if(_o == null)
                return default(Offset<ConsumerScore>);
            var _producer_scores = default(VectorOffset);
            if(_o.ProducerScores != null)
            {
                var __producer_scores = _o.ProducerScores.ToArray();
                _producer_scores = CreateProducerScoresVector(builder, __producer_scores);
            }
            return CreateConsumerScore(
              builder,
              _o.Score,
              _o.ProducerScore,
              _producer_scores);
        }
    }
}
