// <auto-generated>
//  automatically generated by the FlatBuffers compiler, do not modify
// </auto-generated>

using System;
using FlatBuffers.RtpParameters;
using Google.FlatBuffers;
using MediasoupSharp.FlatBuffers.RtpStream.T;

namespace FlatBuffers.RtpStream
{
    public struct BaseStats : IFlatbufferObject
    {
        private Table __p;
        public ByteBuffer ByteBuffer { get { return __p.bb; } }
        public static void ValidateVersion() { FlatBufferConstants.FLATBUFFERS_23_5_26(); }
        public static BaseStats GetRootAsBaseStats(ByteBuffer _bb) { return GetRootAsBaseStats(_bb, new BaseStats()); }
        public static BaseStats GetRootAsBaseStats(ByteBuffer _bb, BaseStats obj) { return (obj.__assign(_bb.GetInt(_bb.Position) + _bb.Position, _bb)); }
        public void __init(int _i, ByteBuffer _bb) { __p = new Table(_i, _bb); }
        public BaseStats __assign(int _i, ByteBuffer _bb) { __init(_i, _bb); return this; }

        public ulong Timestamp { get { int o = __p.__offset(4); return o != 0 ? __p.bb.GetUlong(o + __p.bb_pos) : (ulong)0; } }
        public uint Ssrc { get { int o = __p.__offset(6); return o != 0 ? __p.bb.GetUint(o + __p.bb_pos) : (uint)0; } }
        public MediaKind Kind { get { int o = __p.__offset(8); return o != 0 ? (MediaKind)__p.bb.Get(o + __p.bb_pos) : MediaKind.audio; } }
        public string MimeType { get { int o = __p.__offset(10); return o != 0 ? __p.__string(o + __p.bb_pos) : null; } }
#if ENABLE_SPAN_T
  public Span<byte> GetMimeTypeBytes() { return __p.__vector_as_span<byte>(10, 1); }
#else
        public ArraySegment<byte>? GetMimeTypeBytes() { return __p.__vector_as_arraysegment(10); }
#endif
        public byte[] GetMimeTypeArray() { return __p.__vector_as_array<byte>(10); }
        public ulong PacketsLost { get { int o = __p.__offset(12); return o != 0 ? __p.bb.GetUlong(o + __p.bb_pos) : (ulong)0; } }
        public byte FractionLost { get { int o = __p.__offset(14); return o != 0 ? __p.bb.Get(o + __p.bb_pos) : (byte)0; } }
        public ulong PacketsDiscarded { get { int o = __p.__offset(16); return o != 0 ? __p.bb.GetUlong(o + __p.bb_pos) : (ulong)0; } }
        public ulong PacketsRetransmitted { get { int o = __p.__offset(18); return o != 0 ? __p.bb.GetUlong(o + __p.bb_pos) : (ulong)0; } }
        public ulong PacketsRepaired { get { int o = __p.__offset(20); return o != 0 ? __p.bb.GetUlong(o + __p.bb_pos) : (ulong)0; } }
        public ulong NackCount { get { int o = __p.__offset(22); return o != 0 ? __p.bb.GetUlong(o + __p.bb_pos) : (ulong)0; } }
        public ulong NackPacketCount { get { int o = __p.__offset(24); return o != 0 ? __p.bb.GetUlong(o + __p.bb_pos) : (ulong)0; } }
        public ulong PliCount { get { int o = __p.__offset(26); return o != 0 ? __p.bb.GetUlong(o + __p.bb_pos) : (ulong)0; } }
        public ulong FirCount { get { int o = __p.__offset(28); return o != 0 ? __p.bb.GetUlong(o + __p.bb_pos) : (ulong)0; } }
        public byte Score { get { int o = __p.__offset(30); return o != 0 ? __p.bb.Get(o + __p.bb_pos) : (byte)0; } }
        public string Rid { get { int o = __p.__offset(32); return o != 0 ? __p.__string(o + __p.bb_pos) : null; } }
#if ENABLE_SPAN_T
  public Span<byte> GetRidBytes() { return __p.__vector_as_span<byte>(32, 1); }
#else
        public ArraySegment<byte>? GetRidBytes() { return __p.__vector_as_arraysegment(32); }
#endif
        public byte[] GetRidArray() { return __p.__vector_as_array<byte>(32); }
        public uint? RtxSsrc { get { int o = __p.__offset(34); return o != 0 ? __p.bb.GetUint(o + __p.bb_pos) : (uint?)null; } }
        public ulong RtxPacketsDiscarded { get { int o = __p.__offset(36); return o != 0 ? __p.bb.GetUlong(o + __p.bb_pos) : (ulong)0; } }
        public float RoundTripTime { get { int o = __p.__offset(38); return o != 0 ? __p.bb.GetFloat(o + __p.bb_pos) : (float)0.0f; } }

        public static Offset<BaseStats> CreateBaseStats(FlatBufferBuilder builder,
                                                        ulong timestamp = 0,
                                                        uint ssrc = 0,
                                                        MediaKind kind = MediaKind.audio,
                                                        StringOffset mime_typeOffset = default(StringOffset),
                                                        ulong packets_lost = 0,
                                                        byte fraction_lost = 0,
                                                        ulong packets_discarded = 0,
                                                        ulong packets_retransmitted = 0,
                                                        ulong packets_repaired = 0,
                                                        ulong nack_count = 0,
                                                        ulong nack_packet_count = 0,
                                                        ulong pli_count = 0,
                                                        ulong fir_count = 0,
                                                        byte score = 0,
                                                        StringOffset ridOffset = default(StringOffset),
                                                        uint? rtx_ssrc = null,
                                                        ulong rtx_packets_discarded = 0,
                                                        float round_trip_time = 0.0f)
        {
            builder.StartTable(18);
            BaseStats.AddRtxPacketsDiscarded(builder, rtx_packets_discarded);
            BaseStats.AddFirCount(builder, fir_count);
            BaseStats.AddPliCount(builder, pli_count);
            BaseStats.AddNackPacketCount(builder, nack_packet_count);
            BaseStats.AddNackCount(builder, nack_count);
            BaseStats.AddPacketsRepaired(builder, packets_repaired);
            BaseStats.AddPacketsRetransmitted(builder, packets_retransmitted);
            BaseStats.AddPacketsDiscarded(builder, packets_discarded);
            BaseStats.AddPacketsLost(builder, packets_lost);
            BaseStats.AddTimestamp(builder, timestamp);
            BaseStats.AddRoundTripTime(builder, round_trip_time);
            BaseStats.AddRtxSsrc(builder, rtx_ssrc);
            BaseStats.AddRid(builder, ridOffset);
            BaseStats.AddMimeType(builder, mime_typeOffset);
            BaseStats.AddSsrc(builder, ssrc);
            BaseStats.AddScore(builder, score);
            BaseStats.AddFractionLost(builder, fraction_lost);
            BaseStats.AddKind(builder, kind);
            return BaseStats.EndBaseStats(builder);
        }

        public static void StartBaseStats(FlatBufferBuilder builder) { builder.StartTable(18); }
        public static void AddTimestamp(FlatBufferBuilder builder, ulong timestamp) { builder.AddUlong(0, timestamp, 0); }
        public static void AddSsrc(FlatBufferBuilder builder, uint ssrc) { builder.AddUint(1, ssrc, 0); }
        public static void AddKind(FlatBufferBuilder builder, MediaKind kind) { builder.AddByte(2, (byte)kind, 0); }
        public static void AddMimeType(FlatBufferBuilder builder, StringOffset mimeTypeOffset) { builder.AddOffset(3, mimeTypeOffset.Value, 0); }
        public static void AddPacketsLost(FlatBufferBuilder builder, ulong packetsLost) { builder.AddUlong(4, packetsLost, 0); }
        public static void AddFractionLost(FlatBufferBuilder builder, byte fractionLost) { builder.AddByte(5, fractionLost, 0); }
        public static void AddPacketsDiscarded(FlatBufferBuilder builder, ulong packetsDiscarded) { builder.AddUlong(6, packetsDiscarded, 0); }
        public static void AddPacketsRetransmitted(FlatBufferBuilder builder, ulong packetsRetransmitted) { builder.AddUlong(7, packetsRetransmitted, 0); }
        public static void AddPacketsRepaired(FlatBufferBuilder builder, ulong packetsRepaired) { builder.AddUlong(8, packetsRepaired, 0); }
        public static void AddNackCount(FlatBufferBuilder builder, ulong nackCount) { builder.AddUlong(9, nackCount, 0); }
        public static void AddNackPacketCount(FlatBufferBuilder builder, ulong nackPacketCount) { builder.AddUlong(10, nackPacketCount, 0); }
        public static void AddPliCount(FlatBufferBuilder builder, ulong pliCount) { builder.AddUlong(11, pliCount, 0); }
        public static void AddFirCount(FlatBufferBuilder builder, ulong firCount) { builder.AddUlong(12, firCount, 0); }
        public static void AddScore(FlatBufferBuilder builder, byte score) { builder.AddByte(13, score, 0); }
        public static void AddRid(FlatBufferBuilder builder, StringOffset ridOffset) { builder.AddOffset(14, ridOffset.Value, 0); }
        public static void AddRtxSsrc(FlatBufferBuilder builder, uint? rtxSsrc) { builder.AddUint(15, rtxSsrc); }
        public static void AddRtxPacketsDiscarded(FlatBufferBuilder builder, ulong rtxPacketsDiscarded) { builder.AddUlong(16, rtxPacketsDiscarded, 0); }
        public static void AddRoundTripTime(FlatBufferBuilder builder, float roundTripTime) { builder.AddFloat(17, roundTripTime, 0.0f); }
        public static Offset<BaseStats> EndBaseStats(FlatBufferBuilder builder)
        {
            int o = builder.EndTable();
            builder.Required(o, 10);  // mime_type
            return new Offset<BaseStats>(o);
        }
        public BaseStatsT UnPack()
        {
            var _o = new BaseStatsT();
            this.UnPackTo(_o);
            return _o;
        }
        public void UnPackTo(BaseStatsT _o)
        {
            _o.Timestamp = this.Timestamp;
            _o.Ssrc = this.Ssrc;
            _o.Kind = this.Kind;
            _o.MimeType = this.MimeType;
            _o.PacketsLost = this.PacketsLost;
            _o.FractionLost = this.FractionLost;
            _o.PacketsDiscarded = this.PacketsDiscarded;
            _o.PacketsRetransmitted = this.PacketsRetransmitted;
            _o.PacketsRepaired = this.PacketsRepaired;
            _o.NackCount = this.NackCount;
            _o.NackPacketCount = this.NackPacketCount;
            _o.PliCount = this.PliCount;
            _o.FirCount = this.FirCount;
            _o.Score = this.Score;
            _o.Rid = this.Rid;
            _o.RtxSsrc = this.RtxSsrc;
            _o.RtxPacketsDiscarded = this.RtxPacketsDiscarded;
            _o.RoundTripTime = this.RoundTripTime;
        }
        public static Offset<BaseStats> Pack(FlatBufferBuilder builder, BaseStatsT _o)
        {
            if(_o == null)
                return default(Offset<BaseStats>);
            var _mime_type = _o.MimeType == null ? default(StringOffset) : builder.CreateString(_o.MimeType);
            var _rid = _o.Rid == null ? default(StringOffset) : builder.CreateString(_o.Rid);
            return CreateBaseStats(
              builder,
              _o.Timestamp,
              _o.Ssrc,
              _o.Kind,
              _mime_type,
              _o.PacketsLost,
              _o.FractionLost,
              _o.PacketsDiscarded,
              _o.PacketsRetransmitted,
              _o.PacketsRepaired,
              _o.NackCount,
              _o.NackPacketCount,
              _o.PliCount,
              _o.FirCount,
              _o.Score,
              _rid,
              _o.RtxSsrc,
              _o.RtxPacketsDiscarded,
              _o.RoundTripTime);
        }
    }
}
