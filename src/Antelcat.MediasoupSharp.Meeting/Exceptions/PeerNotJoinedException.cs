namespace Antelcat.MediasoupSharp.Meeting.Exceptions
{
    public class PeerNotJoinedException : MeetingException
    {
        public PeerNotJoinedException(string tag, string peerId) : base($"{tag} | Peer:{peerId} is not joined.")
        {
        }

        public PeerNotJoinedException(string message) : base(message)
        {
        }

        public PeerNotJoinedException()
        {
        }

        public PeerNotJoinedException(string? message, System.Exception? innerException) : base(message, innerException)
        {
        }
    }
}
