using MediasoupSharp.Producer;

namespace MediasoupSharp.RtpObserver;

public record RtpObserverConstructorOptions<TRtpObserverAppData>
{
    internal RtpObserverObserverInternal Internal { get; set; }
    internal Channel.Channel             Channel  { get; set; }
    internal PayloadChannel.PayloadChannel PayloadChannel { get; set; }
    public TRtpObserverAppData? AppData { get; set; }
    public Func<string, IProducer?> GetProducerById { get; set; }
}
