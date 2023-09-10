namespace MediasoupSharp.Router;

public class PipeToRouterResult
{
    /// <summary>
    /// The Consumer created in the current Router.
    /// </summary>
    internal Consumer.Consumer? PipeConsumer { get; set; }

    /// <summary>
    /// The Producer created in the target Router.
    /// </summary>
    internal Producer.Producer? PipeProducer { get; set; }

    /// <summary>
    /// The DataConsumer created in the current Router.
    /// </summary>
    internal DataConsumer.DataConsumer? PipeDataConsumer { get; set; }

    /// <summary>
    /// The DataProducer created in the target Router.
    /// </summary>
    internal DataProducer.DataProducer? PipeDataProducer { get; set; }
}