﻿namespace MediasoupSharp.AudioLevelObserver;

public class AudioLevelObserverVolume
{
    /// <summary>
    /// The audio Producer instance.
    /// </summary>
    public Producer.Producer Producer { get; set; }

    /// <summary>
    /// The average volume (in dBvo from -127 to 0) of the audio Producer in the
    /// last interval.
    /// </summary>
    public int Volume { get; set; }
}