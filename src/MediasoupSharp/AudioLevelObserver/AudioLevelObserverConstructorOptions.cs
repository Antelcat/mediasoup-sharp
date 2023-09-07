using MediasoupSharp.RtpObserver;

namespace MediasoupSharp.AudioLevelObserver;

public record AudioLevelObserverConstructorOptions<TAudioLevelObserverAppData>
    : RtpObserverConstructorOptions<TAudioLevelObserverAppData>;