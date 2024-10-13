// <auto-generated>
//  automatically generated by the FlatBuffers compiler, do not modify
// </auto-generated>

using Google.FlatBuffers;
using System.Text.Json.Serialization;
using MediasoupSharp.FlatBuffers.ActiveSpeakerObserver.T;

namespace FlatBuffers.ActiveSpeakerObserver
{
    public struct ActiveSpeakerObserverOptions : IFlatbufferObject
    {
        private Table __p;
        public ByteBuffer ByteBuffer { get { return __p.bb; } }
        public static void ValidateVersion() { FlatBufferConstants.FLATBUFFERS_23_5_26(); }
        public static ActiveSpeakerObserverOptions GetRootAsActiveSpeakerObserverOptions(ByteBuffer _bb) { return GetRootAsActiveSpeakerObserverOptions(_bb, new ActiveSpeakerObserverOptions()); }
        public static ActiveSpeakerObserverOptions GetRootAsActiveSpeakerObserverOptions(ByteBuffer _bb, ActiveSpeakerObserverOptions obj) { return (obj.__assign(_bb.GetInt(_bb.Position) + _bb.Position, _bb)); }
        public void __init(int _i, ByteBuffer _bb) { __p = new Table(_i, _bb); }
        public ActiveSpeakerObserverOptions __assign(int _i, ByteBuffer _bb) { __init(_i, _bb); return this; }

        public ushort Interval { get { int o = __p.__offset(4); return o != 0 ? __p.bb.GetUshort(o + __p.bb_pos) : (ushort)0; } }

        public static Offset<ActiveSpeakerObserverOptions> CreateActiveSpeakerObserverOptions(FlatBufferBuilder builder,
                                                                                              ushort interval = 0)
        {
            builder.StartTable(1);
            ActiveSpeakerObserverOptions.AddInterval(builder, interval);
            return ActiveSpeakerObserverOptions.EndActiveSpeakerObserverOptions(builder);
        }

        public static void StartActiveSpeakerObserverOptions(FlatBufferBuilder builder) { builder.StartTable(1); }
        public static void AddInterval(FlatBufferBuilder builder, ushort interval) { builder.AddUshort(0, interval, 0); }
        public static Offset<ActiveSpeakerObserverOptions> EndActiveSpeakerObserverOptions(FlatBufferBuilder builder)
        {
            int o = builder.EndTable();
            return new Offset<ActiveSpeakerObserverOptions>(o);
        }
        public ActiveSpeakerObserverOptionsT UnPack()
        {
            var _o = new ActiveSpeakerObserverOptionsT();
            this.UnPackTo(_o);
            return _o;
        }
        public void UnPackTo(ActiveSpeakerObserverOptionsT _o)
        {
            _o.Interval = this.Interval;
        }
        public static Offset<ActiveSpeakerObserverOptions> Pack(FlatBufferBuilder builder, ActiveSpeakerObserverOptionsT _o)
        {
            if(_o == null)
                return default(Offset<ActiveSpeakerObserverOptions>);
            return CreateActiveSpeakerObserverOptions(
              builder,
              _o.Interval);
        }
    }
}
