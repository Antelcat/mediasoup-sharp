namespace MediasoupSharp.Demo.Models
{
    public class PeerProduceResult
    {
        /// <summary>
        /// Producer
        /// </summary>
        public Producer.Producer Producer { get; set; }

        /// <summary>
        /// PullPaddings
        /// </summary>
        public PullPadding[] PullPaddings { get; set; }
    }
}
