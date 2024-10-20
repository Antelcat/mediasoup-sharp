namespace MediasoupSharp.Demo.SignalR.Models
{
    public interface IHubClient
    {
        Task Notify(MeetingNotification notification);
    }
}
