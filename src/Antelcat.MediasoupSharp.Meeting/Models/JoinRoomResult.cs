namespace Antelcat.MediasoupSharp.Meeting.Models
{
    public class JoinRoomResult
    {
        public Peer SelfPeer { get; set; }

        public Peer[] Peers { get; set; }
    }
}
