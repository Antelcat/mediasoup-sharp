using MediasoupSharp.DataProducer;
using MediasoupSharp.DirectTransport;

namespace MediasoupSharp.Demo.Lib;

public class Bot(DirectTransport.DirectTransport transport, DataProducer.DataProducer dataProducer)
{
    public static async Task<Bot> CreateAsync(Router.Router mediasoupRouter)
    {
        var transport = await mediasoupRouter.CreateDirectTransportAsync(new DirectTransportOptions
        {
            MaxMessageSize = 512
        });

        var dataProducer = await transport.ProduceDataAsync(new DataProducerOptions
        {
            Label = "bot"
        });

        var bot = new Bot(transport, dataProducer);

        return bot;
    }

    public DataProducer.DataProducer DataProducer => dataProducer;

    public void Close()
    {
        //
    }

    public async Task HandlePeerDataProducerAsync(string dataProducerId)
    {
        
    }
}