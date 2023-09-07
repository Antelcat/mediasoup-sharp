namespace MediasoupSharp.RtpObserver;

public record RtpObserverConstructorOptions<TRtpObserverAppData>
{
    public RtpObserverObserverInternal Internal { get; set; }
    public Channel.Channel Channel { get; set; }
    public PayloadChannel.PayloadChannel PayloadChannel { get; set; }
    public TRtpObserverAppData? AppData { get; set; }
    public Func<string, Producer.Producer?> GetProducerById { get; set; }
}
