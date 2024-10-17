using FBS.Consumer;

namespace MediasoupSharp.ClientRequest;

public class SetConsumerPreferedLayersRequest : SetPreferredLayersRequestT
{
    public string ConsumerId { get; set; }
}