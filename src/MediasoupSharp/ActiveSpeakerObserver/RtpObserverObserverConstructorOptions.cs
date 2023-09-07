using MediasoupSharp.RtpObserver;

namespace MediasoupSharp.ActiveSpeakerObserver;

public record RtpObserverObserverConstructorOptions<TActiveSpeakerObserverAppData>(
        RtpObserverObserverInternal Internal,
        Channel.Channel Channel,
        PayloadChannel.PayloadChannel PayloadChannel,
        TActiveSpeakerObserverAppData? AppData,
        Func<string, Producer.Producer?> GetProducerById)
    : RtpObserverConstructorOptions<TActiveSpeakerObserverAppData>(
        Internal,
        Channel,
        PayloadChannel,
        AppData,
        GetProducerById);
