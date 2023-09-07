namespace MediasoupSharp.RtpObserver;

public record RtpObserverConstructorOptions<TRtpObserverAppData>(
    RtpObserverObserverInternal Internal,
    Channel.Channel Channel,
    PayloadChannel.PayloadChannel PayloadChannel,
    TRtpObserverAppData? AppData,
    Func<string, Producer.Producer?> GetProducerById);
