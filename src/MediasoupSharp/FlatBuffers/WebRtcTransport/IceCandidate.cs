// <auto-generated>
//  automatically generated by the FlatBuffers compiler, do not modify
// </auto-generated>

using System;
using System.Text.Json.Serialization;
using FlatBuffers.Transport;
using Google.FlatBuffers;
using MediasoupSharp.FlatBuffers.WebRtcTransport.T;

namespace FlatBuffers.WebRtcTransport
{
    public struct IceCandidate : IFlatbufferObject
    {
        private Table __p;
        public ByteBuffer ByteBuffer { get { return __p.bb; } }
        public static void ValidateVersion() { FlatBufferConstants.FLATBUFFERS_23_5_26(); }
        public static IceCandidate GetRootAsIceCandidate(ByteBuffer _bb) { return GetRootAsIceCandidate(_bb, new IceCandidate()); }
        public static IceCandidate GetRootAsIceCandidate(ByteBuffer _bb, IceCandidate obj) { return (obj.__assign(_bb.GetInt(_bb.Position) + _bb.Position, _bb)); }
        public void __init(int _i, ByteBuffer _bb) { __p = new Table(_i, _bb); }
        public IceCandidate __assign(int _i, ByteBuffer _bb) { __init(_i, _bb); return this; }

        public string Foundation { get { int o = __p.__offset(4); return o != 0 ? __p.__string(o + __p.bb_pos) : null; } }
#if ENABLE_SPAN_T
  public Span<byte> GetFoundationBytes() { return __p.__vector_as_span<byte>(4, 1); }
#else
        public ArraySegment<byte>? GetFoundationBytes() { return __p.__vector_as_arraysegment(4); }
#endif
        public byte[] GetFoundationArray() { return __p.__vector_as_array<byte>(4); }
        public uint Priority { get { int o = __p.__offset(6); return o != 0 ? __p.bb.GetUint(o + __p.bb_pos) : (uint)0; } }
        public string Ip { get { int o = __p.__offset(8); return o != 0 ? __p.__string(o + __p.bb_pos) : null; } }
#if ENABLE_SPAN_T
  public Span<byte> GetIpBytes() { return __p.__vector_as_span<byte>(8, 1); }
#else
        public ArraySegment<byte>? GetIpBytes() { return __p.__vector_as_arraysegment(8); }
#endif
        public byte[] GetIpArray() { return __p.__vector_as_array<byte>(8); }
        public Protocol Protocol { get { int o = __p.__offset(10); return o != 0 ? (Protocol)__p.bb.Get(o + __p.bb_pos) : Protocol.UDP; } }
        public ushort Port { get { int o = __p.__offset(12); return o != 0 ? __p.bb.GetUshort(o + __p.bb_pos) : (ushort)0; } }
        public IceCandidateType Type { get { int o = __p.__offset(14); return o != 0 ? (IceCandidateType)__p.bb.Get(o + __p.bb_pos) : IceCandidateType.HOST; } }
        public IceCandidateTcpType? TcpType { get { int o = __p.__offset(16); return o != 0 ? (IceCandidateTcpType)__p.bb.Get(o + __p.bb_pos) : (IceCandidateTcpType?)null; } }

        public static Offset<IceCandidate> CreateIceCandidate(FlatBufferBuilder builder,
                                                              StringOffset foundationOffset = default(StringOffset),
                                                              uint priority = 0,
                                                              StringOffset ipOffset = default(StringOffset),
                                                              Protocol protocol = Protocol.UDP,
                                                              ushort port = 0,
                                                              IceCandidateType type = IceCandidateType.HOST,
                                                              IceCandidateTcpType? tcp_type = null)
        {
            builder.StartTable(7);
            IceCandidate.AddIp(builder, ipOffset);
            IceCandidate.AddPriority(builder, priority);
            IceCandidate.AddFoundation(builder, foundationOffset);
            IceCandidate.AddPort(builder, port);
            IceCandidate.AddTcpType(builder, tcp_type);
            IceCandidate.AddType(builder, type);
            IceCandidate.AddProtocol(builder, protocol);
            return IceCandidate.EndIceCandidate(builder);
        }

        public static void StartIceCandidate(FlatBufferBuilder builder) { builder.StartTable(7); }
        public static void AddFoundation(FlatBufferBuilder builder, StringOffset foundationOffset) { builder.AddOffset(0, foundationOffset.Value, 0); }
        public static void AddPriority(FlatBufferBuilder builder, uint priority) { builder.AddUint(1, priority, 0); }
        public static void AddIp(FlatBufferBuilder builder, StringOffset ipOffset) { builder.AddOffset(2, ipOffset.Value, 0); }
        public static void AddProtocol(FlatBufferBuilder builder, Protocol protocol) { builder.AddByte(3, (byte)protocol, 1); }
        public static void AddPort(FlatBufferBuilder builder, ushort port) { builder.AddUshort(4, port, 0); }
        public static void AddType(FlatBufferBuilder builder, IceCandidateType type) { builder.AddByte(5, (byte)type, 0); }
        public static void AddTcpType(FlatBufferBuilder builder, IceCandidateTcpType? tcpType) { builder.AddByte(6, (byte?)tcpType); }
        public static Offset<IceCandidate> EndIceCandidate(FlatBufferBuilder builder)
        {
            int o = builder.EndTable();
            builder.Required(o, 4);  // foundation
            builder.Required(o, 8);  // ip
            return new Offset<IceCandidate>(o);
        }
        public IceCandidateT UnPack()
        {
            var _o = new IceCandidateT();
            this.UnPackTo(_o);
            return _o;
        }
        public void UnPackTo(IceCandidateT _o)
        {
            _o.Foundation = this.Foundation;
            _o.Priority = this.Priority;
            _o.Ip = this.Ip;
            _o.Protocol = this.Protocol;
            _o.Port = this.Port;
            _o.Type = this.Type;
            _o.TcpType = this.TcpType;
        }
        public static Offset<IceCandidate> Pack(FlatBufferBuilder builder, IceCandidateT _o)
        {
            if(_o == null)
                return default(Offset<IceCandidate>);
            var _foundation = _o.Foundation == null ? default(StringOffset) : builder.CreateString(_o.Foundation);
            var _ip = _o.Ip == null ? default(StringOffset) : builder.CreateString(_o.Ip);
            return CreateIceCandidate(
              builder,
              _foundation,
              _o.Priority,
              _ip,
              _o.Protocol,
              _o.Port,
              _o.Type,
              _o.TcpType);
        }
    }
}
