// <auto-generated>
//  automatically generated by the FlatBuffers compiler, do not modify
// </auto-generated>

using System.Text.Json.Serialization;
using Google.FlatBuffers;

namespace FlatBuffers.RtpStream
{
    public struct Stats : IFlatbufferObject
    {
        private Table __p;
        public ByteBuffer ByteBuffer { get { return __p.bb; } }
        public static void ValidateVersion() { FlatBufferConstants.FLATBUFFERS_23_5_26(); }
        public static Stats GetRootAsStats(ByteBuffer _bb) { return GetRootAsStats(_bb, new Stats()); }
        public static Stats GetRootAsStats(ByteBuffer _bb, Stats obj) { return (obj.__assign(_bb.GetInt(_bb.Position) + _bb.Position, _bb)); }
        public void __init(int _i, ByteBuffer _bb) { __p = new Table(_i, _bb); }
        public Stats __assign(int _i, ByteBuffer _bb) { __init(_i, _bb); return this; }

        public StatsData DataType { get { int o = __p.__offset(4); return o != 0 ? (StatsData)__p.bb.Get(o + __p.bb_pos) : StatsData.NONE; } }
        public TTable? Data<TTable>() where TTable : struct, IFlatbufferObject { int o = __p.__offset(6); return o != 0 ? (TTable?)__p.__union<TTable>(o + __p.bb_pos) : null; }
        public BaseStats DataAsBaseStats() { return Data<BaseStats>().Value; }
        public RecvStats DataAsRecvStats() { return Data<RecvStats>().Value; }
        public SendStats DataAsSendStats() { return Data<SendStats>().Value; }

        public static Offset<Stats> CreateStats(FlatBufferBuilder builder,
                                                StatsData data_type = StatsData.NONE,
                                                int dataOffset = 0)
        {
            builder.StartTable(2);
            Stats.AddData(builder, dataOffset);
            Stats.AddDataType(builder, data_type);
            return Stats.EndStats(builder);
        }

        public static void StartStats(FlatBufferBuilder builder) { builder.StartTable(2); }
        public static void AddDataType(FlatBufferBuilder builder, StatsData dataType) { builder.AddByte(0, (byte)dataType, 0); }
        public static void AddData(FlatBufferBuilder builder, int dataOffset) { builder.AddOffset(1, dataOffset, 0); }
        public static Offset<Stats> EndStats(FlatBufferBuilder builder)
        {
            int o = builder.EndTable();
            builder.Required(o, 6);  // data
            return new Offset<Stats>(o);
        }
        public StatsT UnPack()
        {
            var _o = new StatsT();
            this.UnPackTo(_o);
            return _o;
        }
        public void UnPackTo(StatsT _o)
        {
            _o.Data = new StatsDataUnion();
            _o.Data.Type = this.DataType;
            switch(this.DataType)
            {
                default:
                    break;
                case StatsData.BaseStats:
                    _o.Data.Value = this.Data<BaseStats>().HasValue ? this.Data<BaseStats>().Value.UnPack() : null;
                    break;
                case StatsData.RecvStats:
                    _o.Data.Value = this.Data<RecvStats>().HasValue ? this.Data<RecvStats>().Value.UnPack() : null;
                    break;
                case StatsData.SendStats:
                    _o.Data.Value = this.Data<SendStats>().HasValue ? this.Data<SendStats>().Value.UnPack() : null;
                    break;
            }
        }
        public static Offset<Stats> Pack(FlatBufferBuilder builder, StatsT _o)
        {
            if(_o == null)
                return default(Offset<Stats>);
            var _data_type = _o.Data == null ? StatsData.NONE : _o.Data.Type;
            var _data = _o.Data == null ? 0 : StatsDataUnion.Pack(builder, _o.Data);
            return CreateStats(
              builder,
              _data_type,
              _data);
        }
    }

    public class StatsT
    {
        private StatsData DataType
        {
            get
            {
                return this.Data != null ? this.Data.Type : StatsData.NONE;
            }
            set
            {
                this.Data = new StatsDataUnion();
                this.Data.Type = value;
            }
        }
        public StatsDataUnion Data { get; set; }
    }
}
