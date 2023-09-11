using MediasoupSharp.RtpObserver;

namespace MediasoupSharp.AudioLevelObserver;

internal record AudioLevelObserverConstructorOptions<TAudioLevelObserverAppData>
    : RtpObserverConstructorOptions<TAudioLevelObserverAppData>;