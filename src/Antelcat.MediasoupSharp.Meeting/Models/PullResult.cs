namespace Antelcat.MediasoupSharp.Meeting.Models
{
    public class PullResult
    {
        public Peer ConsumePeer { get; set; }

        public Peer ProducePeer { get; set; }

        public Producer.Producer[] ExistsProducers { get; set; }

        public HashSet<string> Sources { get; set; }
    }
}
