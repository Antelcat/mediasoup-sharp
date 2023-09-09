namespace MediasoupSharp.Transport;

public record TransportObserverEvents
{
    public   List<object>                     Close           { get; set; } = new();
    internal Tuple<Producer.Producer>         Newproducer     { get; set; }
    internal Tuple<Consumer.Consumer>         Newconsumer     { get; set; }
    internal Tuple<DataProducer.DataProducer> Newdataproducer { get; set; }
    internal Tuple<DataConsumer.DataConsumer> Newdataconsumer { get; set; }
    public   Tuple<TransportTraceEventData>   Trace           { get; set; }
}