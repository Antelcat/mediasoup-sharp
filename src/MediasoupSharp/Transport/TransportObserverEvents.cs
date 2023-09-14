using MediasoupSharp.Consumer;
using MediasoupSharp.DataConsumer;
using MediasoupSharp.DataProducer;
using MediasoupSharp.Producer;

namespace MediasoupSharp.Transport;

public record TransportObserverEvents
{
    public   List<object>                   Close           { get; set; } = new();
    internal Tuple<IProducer>               Newproducer     { get; set; }
    internal Tuple<IConsumer>               Newconsumer     { get; set; }
    internal Tuple<IDataProducer>           Newdataproducer { get; set; }
    internal Tuple<IDataConsumer>           Newdataconsumer { get; set; }
    public   Tuple<TransportTraceEventData> Trace           { get; set; }
}