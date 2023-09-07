using MediasoupSharp.RtpObserver;

namespace MediasoupSharp.AudioLevelObserver;

public record AudioLevelObserverConstructorOptions<TAudioLevelObserverAppData>(
        RtpObserverObserverInternal Internal, 
        Channel.Channel Channel, 
        PayloadChannel.PayloadChannel PayloadChannel, 
        TAudioLevelObserverAppData? AppData, 
        Func<string, Producer.Producer?> GetProducerById) 
    : RtpObserverConstructorOptions<TAudioLevelObserverAppData>(
        Internal, 
        Channel, 
        PayloadChannel, 
        AppData, 
        GetProducerById);