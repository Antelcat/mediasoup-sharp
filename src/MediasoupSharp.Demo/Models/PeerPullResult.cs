namespace MediasoupSharp.Demo.Models
{
    public class PeerPullResult
    {
        public Producer.Producer[] ExistsProducers { get; set; }

        public HashSet<string> ProduceSources { get; set; }
    }
}
