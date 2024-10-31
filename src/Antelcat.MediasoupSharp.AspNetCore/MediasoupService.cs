namespace Antelcat.MediasoupSharp.AspNetCore;

public class MediasoupService
{
    public virtual Task OnWorkerCreated(Worker.Worker worker) => Task.CompletedTask;
}