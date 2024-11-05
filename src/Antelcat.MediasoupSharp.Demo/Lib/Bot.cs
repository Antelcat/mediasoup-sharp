using System.Runtime.InteropServices;
using System.Text;
using Antelcat.AspNetCore.ProtooSharp;
using FBS.DataConsumer;

namespace Antelcat.MediasoupSharp.Demo.Lib;

public class Bot<TWorkerAppData>(
    ILogger logger,
    DirectTransport<TWorkerAppData> transport,
    DataProducer<TWorkerAppData> dataProducer)
    where TWorkerAppData : new()
{
    public static async Task<Bot<TWorkerAppData>> CreateAsync(ILoggerFactory loggerFactory, IRouter mediasoupRouter)
    {
        var transport = await mediasoupRouter.CreateDirectTransportAsync<TWorkerAppData>(new()
        {
            MaxMessageSize = 512
        });

        var dataProducer = await transport.ProduceDataAsync<TWorkerAppData>(new()
        {
            Label = "bot"
        });

        var bot = new Bot<TWorkerAppData>(loggerFactory.CreateLogger<Bot<TWorkerAppData>>(), transport, dataProducer);

        return bot;
    }

    public DataProducer<TWorkerAppData> DataProducer => dataProducer;

    public void Close()
    {
        //
    }

    public async Task HandlePeerDataProducerAsync(string dataProducerId, Peer peer)
    {
        var dataConsumer = await transport.ConsumeDataAsync(new DataConsumerOptions<TWorkerAppData>
        {
            DataProducerId = dataProducerId
        });

        dataConsumer.On(static x => x.message, async args =>
        {
            if (args.Ppid != 51)
            {
                logger.LogWarning("ignoring non string message from a Peer");
                return;
            }

            var message = args.Data;

            var text = Encoding.UTF8.GetString(CollectionsMarshal.AsSpan(message));
            logger.LogDebug("SCTP message received [{PeerId}, {Size}]", peer.Id, message.Count);

            // Create a message to send it back to all Peers in behalf of the sending
            // Peer.
            var messageBack = $"{peer.Data.As<Room.PeerData>().DisplayName} said me: {text}";

            await dataProducer.SendAsync(messageBack);
        });
    }
}