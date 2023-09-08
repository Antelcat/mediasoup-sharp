namespace MediasoupSharp.ActiveSpeakerObserver;

[Serializable]
public class ActiveSpeakerObserverOptions<TActiveSpeakerObserverAppData>
{
    public int? Interval { get; set; } = 300;

    /// <summary>
    /// Custom application data.
    /// </summary>
    public TActiveSpeakerObserverAppData? AppData { get; set; }
}