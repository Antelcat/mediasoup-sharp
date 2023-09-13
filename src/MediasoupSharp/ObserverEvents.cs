using MediasoupSharp.Worker;

namespace MediasoupSharp;

public record ObserverEvents
{
    public Tuple<IWorker> Newworker { get; set; }
}