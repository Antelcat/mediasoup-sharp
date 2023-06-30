namespace MediasoupSharp;

public record ActiveSpeakerObserverOptions<TActiveSpeakerObserverAppData>(int Interval,
    TActiveSpeakerObserverAppData AppData)
    where TActiveSpeakerObserverAppData : AppData;

public record ActiveSpeakerObserverDominantSpeaker(Producer Producer);

public record ActiveSpeakerObserver;
