using FBS.Consumer;

namespace Antelcat.MediasoupSharp.ClientRequest;

public class SetConsumerPreferredLayersRequest : SetPreferredLayersRequestT
{
    public string ConsumerId { get; set; }
}