using MediasoupSharp.DataProducer;
using MediasoupSharp.Producer;

namespace MediasoupSharp.Transport;

public record TransportEvents
{
    public List<object> Routerclose       { get; set; } = new();
    public List<object> Listenserverclose { get; set; } = new();

    public Tuple<TransportTraceEventData> Trace { get; set; }

    // Private events.
    private List<object>                     close             { get; set; } = new();
    private Tuple<IProducer>                 newproducer       { get; set; }
    private Tuple<IProducer>                 producerclose     { get; set; }
    private Tuple<IDataProducer> newdataproducer   { get; set; }
    private Tuple<IDataProducer> dataproducerclose { get; set; }
    private List<object>                     listenserverclose { get; set; } = new();
}