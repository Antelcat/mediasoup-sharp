namespace MediasoupSharp.Router;

public record RouterEvents
{
    public  List<object> WorkerClose { get; set; } = new();
    // Private events.
    private List<object> close = new();
}