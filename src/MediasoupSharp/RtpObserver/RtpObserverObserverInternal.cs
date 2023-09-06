using MediasoupSharp.Router;

namespace MediasoupSharp.RtpObserver;

public class RtpObserverObserverInternal : RouterInternal
{
    public RtpObserverObserverInternal(string routerId) : base(routerId)
    { }

    public string RtpObserverId { get; set; } = string.Empty;
}