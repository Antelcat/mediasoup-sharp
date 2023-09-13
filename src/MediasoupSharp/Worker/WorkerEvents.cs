namespace MediasoupSharp.Worker;

public record WorkerEvents
{
    public Tuple<Exception> Died { get; set; }

    private List<object>     success = new();
    private Tuple<Exception> failure;

}