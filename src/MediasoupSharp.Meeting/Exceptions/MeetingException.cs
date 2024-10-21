namespace MediasoupSharp.Meeting.Exceptions
{
    public class MeetingException : Exception
    {
        public MeetingException(string message) : base(message)
        {
        }

        public MeetingException()
        {
        }

        public MeetingException(string? message, Exception? innerException) : base(message, innerException)
        {
        }
    }
}
