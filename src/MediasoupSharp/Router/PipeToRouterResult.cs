using MediasoupSharp.Consumer;
using MediasoupSharp.DataConsumer;
using MediasoupSharp.DataProducer;
using MediasoupSharp.Producer;

namespace MediasoupSharp.Router;

public class PipeToRouterResult
{
    /// <summary>
    /// The Consumer created in the current Router.
    /// </summary>
    internal IConsumer? PipeConsumer { get; set; }

    /// <summary>
    /// The Producer created in the target Router.
    /// </summary>
    internal IProducer? PipeProducer { get; set; }

    /// <summary>
    /// The DataConsumer created in the current Router.
    /// </summary>
    internal IDataConsumer? PipeDataConsumer { get; set; }

    /// <summary>
    /// The DataProducer created in the target Router.
    /// </summary>
    internal IDataProducer? PipeDataProducer { get; set; }
}