namespace Antelcat.MediasoupSharp.Meeting.Models
{
    public class PeerRoomAppDataResult
    {
        public string SelfPeerId { get; set; }

        public Dictionary<string, object> AppData { get; set; }

        public string[] OtherPeerIds { get; set; }
    }
}
