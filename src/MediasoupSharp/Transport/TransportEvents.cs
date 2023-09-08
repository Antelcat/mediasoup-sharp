
namespace MediasoupSharp.Transport;

public record TransportEvents
{
    public List<object> Routerclose { get; set; } = new();
    public List<object> Listenserverclose { get; set; } = new();

    public Tuple<TransportTraceEventData> Trace { get; set; } = new();
    // Private events.
    private List<object> @close { get; set; } = new();
    private Tuple<Producer.Producer> @newproducer { get;set; }
    private Tuple<Producer.Producer> @producerclose { get;set; }
    private Tuple<DataProducer.DataProducer> @newdataproducer { get;set; }
    private Tuple<DataProducer.DataProducer> @dataproducerclose { get;set; }
    private List<object> @listenserverclose { get; set; } = new();
}