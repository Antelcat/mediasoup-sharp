using MediasoupSharp.DataProducer;
using MediasoupSharp.Producer;
using MediasoupSharp.RtpParameters;

namespace MediasoupSharp.Transport;

public record TransportConstructorOptions<TTransportAppData>
{
    internal TransportInternal                        Internal                 { get; set; }
    public   TransportData                            Data                     { get; set; }
    internal Channel.Channel                          Channel                  { get; set; }
    internal PayloadChannel.PayloadChannel            PayloadChannel           { get; set; }
    public   TTransportAppData?                       AppData                  { get; set; }
    public   Func<RtpCapabilities>                    GetRouterRtpCapabilities { get; set; }
    internal Func<string, IProducer?>         GetProducerById          { get; set; }
    internal Func<string, IDataProducer?> GetDataProducerById      { get; set; }
}