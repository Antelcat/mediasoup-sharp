using FlatBuffers.Message;
using Google.FlatBuffers;
using LibuvSharp;
using Microsoft.Extensions.Logging;

namespace MediasoupSharp.Channel;

public class Channel : ChannelBase
{
    #region Constants

    private const int RecvBufferMaxLen = PayloadMaxLen * 2;

    #endregion Constants

    #region Private Fields

    /// <summary>
    /// Unix Socket instance for sending messages to the worker process.
    /// </summary>
    private readonly UVStream producerSocket;

    /// <summary>
    /// Unix Socket instance for receiving messages to the worker process.
    /// </summary>
    private readonly UVStream consumerSocket;

    // TODO: CircularBuffer
    /// <summary>
    /// Buffer for reading messages from the worker.
    /// </summary>
    private readonly byte[] recvBuffer;

    private int recvBufferCount;

    #endregion Private Fields

    public Channel(ILogger<Channel> logger, UVStream producerSocket, UVStream consumerSocket, int processId)
        : base(logger, processId)
    {
        this.producerSocket = producerSocket;
        this.consumerSocket = consumerSocket;

        recvBuffer      = new byte[RecvBufferMaxLen];
        recvBufferCount = 0;

        this.consumerSocket.Data   += ConsumerSocketOnData;
        this.consumerSocket.Closed += ConsumerSocketOnClosed;
        this.consumerSocket.Error  += ConsumerSocketOnError;
        this.producerSocket.Closed += ProducerSocketOnClosed;
        this.producerSocket.Error  += ProducerSocketOnError;
    }

    public override void Cleanup()
    {
        base.Cleanup();

        // Remove event listeners but leave a fake 'error' hander to avoid
        // propagation.
        consumerSocket.Data   -= ConsumerSocketOnData;
        consumerSocket.Closed -= ConsumerSocketOnClosed;
        consumerSocket.Error  -= ConsumerSocketOnError;

        producerSocket.Closed -= ProducerSocketOnClosed;
        producerSocket.Error  -= ProducerSocketOnError;

        // Destroy the socket after a while to allow pending incoming messages.
        // 在 Node.js 实现中，延迟了 200 ms。
        try
        {
            producerSocket.Close();
        }
        catch(Exception ex)
        {
            Logger.LogError(ex, "CloseAsync() | Worker[{WorkerId}] _producerSocket.Close()", WorkerId);
        }

        try
        {
            consumerSocket.Close();
        }
        catch(Exception ex)
        {
            Logger.LogError(ex, "CloseAsync() | Worker[{WorkerId}] _consumerSocket.Close()", WorkerId);
        }
    }

    protected override void SendRequest(Sent sent)
    {
        Loop.Default.Sync(() =>
        {
            try
            {
                // This may throw if closed or remote side ended.
                producerSocket.Write(
                    sent.RequestMessage.Payload,
                    ex =>
                    {
                        if(ex != null)
                        {
                            Logger.LogError(ex, "_producerSocket.Write() | Worker[{WorkerId}] Error", WorkerId);
                            sent.Reject(ex);
                        }
                    }
                );
            }
            catch(Exception ex)
            {
                Logger.LogError(ex, "_producerSocket.Write() | Worker[{WorkerId}] Error", WorkerId);
                sent.Reject(ex);
            }
        });
    }

    protected override void SendNotification(RequestMessage requestMessage)
    {
        Loop.Default.Sync(() =>
        {
            try
            {
                // This may throw if closed or remote side ended.
                producerSocket.Write(
                    requestMessage.Payload,
                    ex =>
                    {
                        if(ex != null)
                        {
                            Logger.LogError(ex, "_producerSocket.Write() | Worker[{WorkerId}] Error", WorkerId);
                        }
                    }
                );
            }
            catch(Exception ex)
            {
                Logger.LogError(ex, "_producerSocket.Write() | Worker[{WorkerId}] Error", WorkerId);
            }
        });
    }

    #region Event handles

    private void ConsumerSocketOnData(ArraySegment<byte> data)
    {
        // 数据回调通过单一线程进入，所以 _recvBuffer 是 Thread-safe 的。
        if(recvBufferCount + data.Count > RecvBufferMaxLen)
        {
            Logger.LogError(
                "ConsumerSocketOnData() | Worker[{WorkerId}] Receiving buffer is full, discarding all data into it",
                WorkerId
            );
            recvBufferCount = 0;
            return;
        }

        Array.Copy(data.Array!, data.Offset, recvBuffer, recvBufferCount, data.Count);
        recvBufferCount += data.Count;

        try
        {
            var readCount = 0;
            while(readCount < recvBufferCount - sizeof(int) - 1)
            {
                var msgLen = BitConverter.ToInt32(recvBuffer, readCount);
                readCount += sizeof(int);
                if(readCount >= recvBufferCount)
                {
                    // Incomplete data.
                    break;
                }

                var messageBytes = new byte[msgLen];
                Array.Copy(recvBuffer, readCount, messageBytes, 0, msgLen);
                readCount += msgLen;

                var buf     = new ByteBuffer(messageBytes);
                var message = Message.GetRootAsMessage(buf);
                ProcessMessage(message);
            }

            var remainingLength = recvBufferCount - readCount;
            if(remainingLength == 0)
            {
                recvBufferCount = 0;
            }
            else
            {
                var temp = new byte[remainingLength];
                Array.Copy(recvBuffer, readCount, temp, 0, remainingLength);
                Array.Copy(temp, 0, recvBuffer, 0, remainingLength);
            }
        }
        catch(Exception ex)
        {
            Logger.LogError(ex, "ConsumerSocketOnData() | Worker[{WorkerId}] Invalid data received from the worker process.", WorkerId);
        }
    }

    private void ConsumerSocketOnClosed()
    {
        Logger.LogDebug("ConsumerSocketOnClosed() | Worker[{WorkerId}] Consumer Channel ended by the worker process", WorkerId);
    }

    private void ConsumerSocketOnError(Exception? exception)
    {
        Logger.LogDebug(exception, "ConsumerSocketOnError() | Worker[{WorkerId}] Consumer Channel error", WorkerId);
    }

    private void ProducerSocketOnClosed()
    {
        Logger.LogDebug("ProducerSocketOnClosed() | Worker[{WorkerId}] Producer Channel ended by the worker process", WorkerId);
    }

    private void ProducerSocketOnError(Exception? exception)
    {
        Logger.LogDebug(exception, "ProducerSocketOnError() | Worker[{WorkerId}] Producer Channel error", WorkerId);
    }

    #endregion Event handles
}
