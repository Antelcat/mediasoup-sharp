namespace MediasoupSharp.ClientRequest;

public class PullRequest
{
    public string PeerId { get; set; }

    public HashSet<string> Sources { get; set; }
}