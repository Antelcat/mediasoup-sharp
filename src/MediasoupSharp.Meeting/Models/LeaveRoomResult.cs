namespace MediasoupSharp.Meeting.Models
{
    public class LeaveRoomResult
    {
        public Peer SelfPeer { get; set; }

        public string[] OtherPeerIds { get; set; }
    }
}
