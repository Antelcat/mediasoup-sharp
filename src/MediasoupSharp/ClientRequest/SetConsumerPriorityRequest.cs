using FBS.Consumer;

namespace MediasoupSharp.ClientRequest;

public class SetConsumerPriorityRequest : SetPriorityRequestT
{
    public string ConsumerId { get; set; }
}