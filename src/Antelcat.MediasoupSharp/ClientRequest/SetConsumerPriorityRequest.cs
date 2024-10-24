using FBS.Consumer;

namespace Antelcat.MediasoupSharp.ClientRequest;

public class SetConsumerPriorityRequest : SetPriorityRequestT
{
    public string ConsumerId { get; set; }
}