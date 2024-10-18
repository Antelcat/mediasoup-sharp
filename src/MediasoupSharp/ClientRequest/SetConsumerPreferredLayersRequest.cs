using FBS.Consumer;

namespace MediasoupSharp.ClientRequest;

public class SetConsumerPreferredLayersRequest : SetPreferredLayersRequestT
{
    public string ConsumerId { get; set; }
}