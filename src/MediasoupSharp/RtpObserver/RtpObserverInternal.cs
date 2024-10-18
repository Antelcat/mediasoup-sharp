namespace MediasoupSharp.RtpObserver;

public class RtpObserverInternal(string routerId, string rtpObserverId)
{
    /// <summary>
    /// Router id.
    /// </summary>
    public string RouterId { get; } = routerId;

    /// <summary>
    /// RtpObserver id.
    /// </summary>
    public string RtpObserverId { get; } = rtpObserverId;
}