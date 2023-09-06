namespace MediasoupSharp.RtpObserver;

public abstract class RtpObserverConstructorOptions<TRtpObserverAppData>
{
    private RtpObserverObserverInternal Internal { get; set; }
    public Channel.Channel Channel { get; set; }
    private PayloadChannel.PayloadChannel PayloadChannel { get; set; }
    private TRtpObserverAppData? AppData { get; set; }
    public abstract Producer.Producer? GetProducerById(string producerId);
}