using System.Text;
using Antelcat.AspNetCore.ProtooSharp;
using Antelcat.MediasoupSharp.DataConsumer;
using Antelcat.MediasoupSharp.DataProducer;
using Antelcat.MediasoupSharp.DirectTransport;

namespace Antelcat.MediasoupSharp.Demo.Lib;

public class Bot(ILogger logger,DirectTransport.DirectTransport transport, DataProducer.DataProducer dataProducer)
{
    
    
    public static async Task<Bot> CreateAsync(ILoggerFactory loggerFactory, Router.Router mediasoupRouter)
    {
        var transport = await mediasoupRouter.CreateDirectTransportAsync(new DirectTransportOptions
        {
            MaxMessageSize = 512
        });

        var dataProducer = await transport.ProduceDataAsync(new DataProducerOptions
        {
            Label = "bot"
        });

        var bot = new Bot(loggerFactory.CreateLogger<Bot>(), transport, dataProducer);

        return bot;
    }

    public DataProducer.DataProducer DataProducer => dataProducer;

    public void Close()
    {
        //
    }

    public async Task HandlePeerDataProducerAsync(string dataProducerId, Peer peer)
    {
        var dataConsumer = await transport.ConsumeDataAsync(new DataConsumerOptions
        {
            DataProducerId = dataProducerId
        });
        
        dataConsumer.On("message", async args =>
        {
            if (args is not [byte[] message, 51])
            {
                logger.LogWarning("ignoring non string message from a Peer");
                return;
            }

            var text = Encoding.UTF8.GetString(message);
            logger.LogDebug("SCTP message received [{PeerId}, {Size}]", peer.Id, message.Length);
            
            // Create a message to send it back to all Peers in behalf of the sending
            // Peer.
            var messageBack = $"{peer.Data.As<Room.PeerData>().DisplayName} said me: {text}";

            await dataProducer.SendAsync(messageBack);
        });
    }
}