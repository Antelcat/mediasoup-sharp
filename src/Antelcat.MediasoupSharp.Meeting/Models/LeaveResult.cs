namespace Antelcat.MediasoupSharp.Meeting.Models
{
    public class LeaveResult
    {
        public Peer SelfPeer { get; set; }

        public string[] OtherPeerIds { get; set; }
    }
}
