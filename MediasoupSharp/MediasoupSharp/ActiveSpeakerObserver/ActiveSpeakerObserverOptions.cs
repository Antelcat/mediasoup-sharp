namespace MediasoupSharp.ActiveSpeakerObserver
{
    public class ActiveSpeakerObserverOptions
    {
        public int? Interval { get; set; } = 300;

        /// <summary>
        /// Custom application data.
        /// </summary>
        public Dictionary<string, object>? AppData { get; set; }
    }
}
