using MediasoupSharp.Router;

namespace MediasoupSharp.RtpObserver;

internal record RtpObserverObserverInternal : RouterInternal
{
    public string RtpObserverId { get; set; } = string.Empty;
}