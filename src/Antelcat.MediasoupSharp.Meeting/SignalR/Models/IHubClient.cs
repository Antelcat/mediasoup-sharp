namespace Antelcat.MediasoupSharp.Meeting.SignalR.Models
{
    public interface IHubClient
    {
        Task Notify(MeetingNotification notification);
    }
}
