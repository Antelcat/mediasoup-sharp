namespace MediasoupSharp;

public record AudioLevelObserverOptions<TAudioLevelObserverAppData>(
    Number? MaxEntries,
    Number? Threshold,
    Number? Interval,
    TAudioLevelObserverAppData? AppData)
    where TAudioLevelObserverAppData : AppData;


public record AudioLevelObserverVolume(Producer Producer,Number Volume);

public record AudioLevelObserverEvents;