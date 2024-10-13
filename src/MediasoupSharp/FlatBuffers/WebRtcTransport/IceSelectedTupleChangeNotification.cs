// <auto-generated>
//  automatically generated by the FlatBuffers compiler, do not modify
// </auto-generated>

using FlatBuffers.Transport;
using Google.FlatBuffers;
using MediasoupSharp.FlatBuffers.WebRtcTransport.T;
using Tuple = FlatBuffers.Transport.Tuple;

namespace FlatBuffers.WebRtcTransport
{
    public struct IceSelectedTupleChangeNotification : IFlatbufferObject
    {
        private Table __p;
        public ByteBuffer ByteBuffer { get { return __p.bb; } }
        public static void ValidateVersion() { FlatBufferConstants.FLATBUFFERS_23_5_26(); }
        public static IceSelectedTupleChangeNotification GetRootAsIceSelectedTupleChangeNotification(ByteBuffer _bb) { return GetRootAsIceSelectedTupleChangeNotification(_bb, new IceSelectedTupleChangeNotification()); }
        public static IceSelectedTupleChangeNotification GetRootAsIceSelectedTupleChangeNotification(ByteBuffer _bb, IceSelectedTupleChangeNotification obj) { return (obj.__assign(_bb.GetInt(_bb.Position) + _bb.Position, _bb)); }
        public void __init(int _i, ByteBuffer _bb) { __p = new Table(_i, _bb); }
        public IceSelectedTupleChangeNotification __assign(int _i, ByteBuffer _bb) { __init(_i, _bb); return this; }

        public Tuple? Tuple { get { int o = __p.__offset(4); return o != 0 ? (Tuple?)(new Tuple()).__assign(__p.__indirect(o + __p.bb_pos), __p.bb) : null; } }

        public static Offset<IceSelectedTupleChangeNotification> CreateIceSelectedTupleChangeNotification(FlatBufferBuilder builder,
                                                                                                          Offset<Tuple> tupleOffset = default(Offset<Tuple>))
        {
            builder.StartTable(1);
            IceSelectedTupleChangeNotification.AddTuple(builder, tupleOffset);
            return IceSelectedTupleChangeNotification.EndIceSelectedTupleChangeNotification(builder);
        }

        public static void StartIceSelectedTupleChangeNotification(FlatBufferBuilder builder) { builder.StartTable(1); }
        public static void AddTuple(FlatBufferBuilder builder, Offset<Tuple> tupleOffset) { builder.AddOffset(0, tupleOffset.Value, 0); }
        public static Offset<IceSelectedTupleChangeNotification> EndIceSelectedTupleChangeNotification(FlatBufferBuilder builder)
        {
            int o = builder.EndTable();
            builder.Required(o, 4);  // tuple
            return new Offset<IceSelectedTupleChangeNotification>(o);
        }
        public IceSelectedTupleChangeNotificationT UnPack()
        {
            var _o = new IceSelectedTupleChangeNotificationT();
            this.UnPackTo(_o);
            return _o;
        }
        public void UnPackTo(IceSelectedTupleChangeNotificationT _o)
        {
            _o.Tuple = this.Tuple.HasValue ? this.Tuple.Value.UnPack() : null;
        }
        public static Offset<IceSelectedTupleChangeNotification> Pack(FlatBufferBuilder builder, IceSelectedTupleChangeNotificationT _o)
        {
            if(_o == null)
                return default(Offset<IceSelectedTupleChangeNotification>);
            var _tuple = _o.Tuple == null ? default(Offset<Tuple>) : Transport.Tuple.Pack(builder, _o.Tuple);
            return CreateIceSelectedTupleChangeNotification(
              builder,
              _tuple);
        }
    }
}
