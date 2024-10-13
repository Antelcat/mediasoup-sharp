using MediasoupSharp.FlatBuffers.Consumer.T;

namespace MediasoupSharp.ClientRequest;

public class SetConsumerPriorityRequest : SetPriorityRequestT
{
    public string ConsumerId { get; set; }
}
