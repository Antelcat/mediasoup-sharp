using MediasoupSharp.FlatBuffers.Consumer.T;

namespace MediasoupSharp.ClientRequest;

public class SetConsumerPreferedLayersRequest : SetPreferredLayersRequestT
{
    public string ConsumerId { get; set; }
}
