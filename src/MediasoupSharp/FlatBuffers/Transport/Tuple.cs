// <auto-generated>
//  automatically generated by the FlatBuffers compiler, do not modify
// </auto-generated>

using System;
using System.Text.Json.Serialization;
using Google.FlatBuffers;

namespace FlatBuffers.Transport
{
    public struct Tuple : IFlatbufferObject
    {
        private Table __p;
        public ByteBuffer ByteBuffer { get { return __p.bb; } }
        public static void ValidateVersion() { FlatBufferConstants.FLATBUFFERS_23_5_26(); }
        public static Tuple GetRootAsTuple(ByteBuffer _bb) { return GetRootAsTuple(_bb, new Tuple()); }
        public static Tuple GetRootAsTuple(ByteBuffer _bb, Tuple obj) { return (obj.__assign(_bb.GetInt(_bb.Position) + _bb.Position, _bb)); }
        public void __init(int _i, ByteBuffer _bb) { __p = new Table(_i, _bb); }
        public Tuple __assign(int _i, ByteBuffer _bb) { __init(_i, _bb); return this; }

        public string LocalIp { get { int o = __p.__offset(4); return o != 0 ? __p.__string(o + __p.bb_pos) : null; } }
#if ENABLE_SPAN_T
  public Span<byte> GetLocalIpBytes() { return __p.__vector_as_span<byte>(4, 1); }
#else
        public ArraySegment<byte>? GetLocalIpBytes() { return __p.__vector_as_arraysegment(4); }
#endif
        public byte[] GetLocalIpArray() { return __p.__vector_as_array<byte>(4); }
        public ushort LocalPort { get { int o = __p.__offset(6); return o != 0 ? __p.bb.GetUshort(o + __p.bb_pos) : (ushort)0; } }
        public string RemoteIp { get { int o = __p.__offset(8); return o != 0 ? __p.__string(o + __p.bb_pos) : null; } }
#if ENABLE_SPAN_T
  public Span<byte> GetRemoteIpBytes() { return __p.__vector_as_span<byte>(8, 1); }
#else
        public ArraySegment<byte>? GetRemoteIpBytes() { return __p.__vector_as_arraysegment(8); }
#endif
        public byte[] GetRemoteIpArray() { return __p.__vector_as_array<byte>(8); }
        public ushort RemotePort { get { int o = __p.__offset(10); return o != 0 ? __p.bb.GetUshort(o + __p.bb_pos) : (ushort)0; } }
        public Protocol Protocol { get { int o = __p.__offset(12); return o != 0 ? (Protocol)__p.bb.Get(o + __p.bb_pos) : Protocol.UDP; } }

        public static Offset<Tuple> CreateTuple(FlatBufferBuilder builder,
                                                StringOffset local_ipOffset = default(StringOffset),
                                                ushort local_port = 0,
                                                StringOffset remote_ipOffset = default(StringOffset),
                                                ushort remote_port = 0,
                                                Protocol protocol = Protocol.UDP)
        {
            builder.StartTable(5);
            Tuple.AddRemoteIp(builder, remote_ipOffset);
            Tuple.AddLocalIp(builder, local_ipOffset);
            Tuple.AddRemotePort(builder, remote_port);
            Tuple.AddLocalPort(builder, local_port);
            Tuple.AddProtocol(builder, protocol);
            return Tuple.EndTuple(builder);
        }

        public static void StartTuple(FlatBufferBuilder builder) { builder.StartTable(5); }
        public static void AddLocalIp(FlatBufferBuilder builder, StringOffset localIpOffset) { builder.AddOffset(0, localIpOffset.Value, 0); }
        public static void AddLocalPort(FlatBufferBuilder builder, ushort localPort) { builder.AddUshort(1, localPort, 0); }
        public static void AddRemoteIp(FlatBufferBuilder builder, StringOffset remoteIpOffset) { builder.AddOffset(2, remoteIpOffset.Value, 0); }
        public static void AddRemotePort(FlatBufferBuilder builder, ushort remotePort) { builder.AddUshort(3, remotePort, 0); }
        public static void AddProtocol(FlatBufferBuilder builder, Protocol protocol) { builder.AddByte(4, (byte)protocol, 1); }
        public static Offset<Tuple> EndTuple(FlatBufferBuilder builder)
        {
            int o = builder.EndTable();
            builder.Required(o, 4);  // local_ip
            return new Offset<Tuple>(o);
        }
        public TupleT UnPack()
        {
            var _o = new TupleT();
            this.UnPackTo(_o);
            return _o;
        }
        public void UnPackTo(TupleT _o)
        {
            _o.LocalIp = this.LocalIp;
            _o.LocalPort = this.LocalPort;
            _o.RemoteIp = this.RemoteIp;
            _o.RemotePort = this.RemotePort;
            _o.Protocol = this.Protocol;
        }
        public static Offset<Tuple> Pack(FlatBufferBuilder builder, TupleT _o)
        {
            if(_o == null)
                return default(Offset<Tuple>);
            var _local_ip = _o.LocalIp == null ? default(StringOffset) : builder.CreateString(_o.LocalIp);
            var _remote_ip = _o.RemoteIp == null ? default(StringOffset) : builder.CreateString(_o.RemoteIp);
            return CreateTuple(
              builder,
              _local_ip,
              _o.LocalPort,
              _remote_ip,
              _o.RemotePort,
              _o.Protocol);
        }
    }
}
