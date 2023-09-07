namespace MediasoupSharp.Channel
{
    public class Sent
    {
        public int Id { get; set; }
        
        public string Method { get; set; }
        public Action<object?> Resolve { get; set; }

        public Action<Exception> Reject { get; set; }

        public Action Close { get; set; }
    }
}
