namespace Antelcat.MediasoupSharp.Meeting.Models
{
    public class ProduceResult
    {
        /// <summary>
        /// ProducerPeer
        /// </summary>
        public Peer ProducerPeer { get; set; }

        /// <summary>
        /// Producer
        /// </summary>
        public Producer.Producer Producer { get; set; }

        /// <summary>
        /// PullPaddingConsumerPeers
        /// </summary>
        public Peer[] PullPaddingConsumerPeers { get; set; }
    }
}
