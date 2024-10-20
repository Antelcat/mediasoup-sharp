namespace MediasoupSharp.Demo.Models
{
    public class JoinRoomResult
    {
        public Peer SelfPeer { get; set; }

        public Peer[] Peers { get; set; }
    }
}
