namespace MediasoupSharp.WebRtcServer;

public record WebRtcServerEvents
{
    public List<object> Workerclose { get; set; } = new();

    private List<object> close = new();
}