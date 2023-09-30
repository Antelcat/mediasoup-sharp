using System.ComponentModel;
using System.Text;
using System.Text.Json;
using LibuvSharp;
using MediasoupSharp.Errors;
using Microsoft.Extensions.Logging;

// ReSharper disable InconsistentNaming

namespace MediasoupSharp.PayloadChannel;

internal class PayloadChannel : EnhancedEventEmitter
{
    private readonly ILogger? logger;
    // Binary Length for a 4194304 bytes payload.
    const int MESSAGE_MAX_LEN = 4194308;
    const int PAYLOAD_MAX_LEN = 4194304;

    private bool closed;

    /// <summary>
    /// Unix Socket instance for sending messages to the worker process.
    /// </summary>
    private readonly Pipe producerSocket;

    /// <summary>
    /// Unix Socket instance for receiving messages to the worker process.
    /// </summary>
    private readonly Pipe consumerSocket;

    // Next id for messages sent to the worker process.
    private uint nextId = 0;

    private readonly Dictionary<uint, Sent> sents = new();

    /// <summary>
    /// Buffer for reading messages from the worker.
    /// </summary>
    private byte[] recvBuffer = Array.Empty<byte>();

    private OngoingNotification? ongoingNotification;

    public PayloadChannel(
        Pipe producerSocket, 
        Pipe consumerSocket,
        ILoggerFactory? loggerFactory = null) 
        : base(loggerFactory)
    {
        logger = loggerFactory?.CreateLogger(GetType());
        
        this.producerSocket = producerSocket;
        this.consumerSocket = consumerSocket;

        // Read PayloadChannel notifications from the worker.
        consumerSocketOnData = bytes =>
        {
            recvBuffer = (recvBuffer.Length == 0 ? bytes : recvBuffer.Concat(bytes)).ToArray();

            if (recvBuffer.Length > PAYLOAD_MAX_LEN)
            {
                logger?.LogError("receiving buffer is full, discarding all data in it");

                // Reset the buffer and exit.
                recvBuffer = Array.Empty<byte>();
                return;
            }

            var msgStart = 0;

            while (true) // eslint-disable-line no-constant-condition
            {
                var readLen = recvBuffer.Length - msgStart;

                if (readLen < 4)
                {
                    // Incomplete data.
                    break;
                }

                var msgLen = BitConverter.ToInt32(recvBuffer, msgStart);

                if (readLen < 4 + msgLen)
                {
                    // Incomplete data.
                    break;
                }

                var payload = recvBuffer[new Range(msgStart + 4, msgStart + 4 + msgLen)];

                msgStart += 4 + msgLen;

                ProcessData(payload);
            }

            if (msgStart != 0)
            {
                recvBuffer = recvBuffer[new Range(msgStart, recvBuffer.Length - 1)];
            }
        };

        consumerSocketOnClosed = () =>
            logger?.LogDebug("Consumer PayloadChannel ended by the worker process");

        consumerSocketOnError = error =>
            logger?.LogError("Consumer PayloadChannel error: {Error}", error);

        producerSocketOnClosed = () =>
            logger?.LogDebug("Producer PayloadChannel ended by the worker process");

        producerSocketOnError = error =>
            logger?.LogError("Producer PayloadChannel error: {Error}", error);

        this.consumerSocket.Data   += consumerSocketOnData;
        this.consumerSocket.Closed += consumerSocketOnClosed;
        this.consumerSocket.Error  += consumerSocketOnError;
        
        this.producerSocket.Closed += producerSocketOnClosed;
        this.producerSocket.Error  += producerSocketOnError;
    }

    public void Close()
    {
        if (closed)
        {
            return;
        }

        logger?.LogDebug("close()");

        closed = true;

        // Remove event listeners but leave a fake "error" hander to avoid
        consumerSocket.Data   -= consumerSocketOnData;
        consumerSocket.Closed -= consumerSocketOnClosed;
        consumerSocket.Error  -= consumerSocketOnError;

        producerSocket.Closed -= producerSocketOnClosed;
        producerSocket.Error  -= producerSocketOnError;

        // Destroy the socket after a while to allow pending incoming messages.
        Task.Delay(200).ContinueWith(_ =>
        {
            try
            {
                producerSocket.Close();
            }
            catch (Exception error)
            {
                //
            }

            try
            {
                consumerSocket.Close();
            }
            catch (Exception error)
            {
                //
            }
        });
    }

    /// <summary>
    /// </summary>
    /// <param name="event"></param>
    /// <param name="handlerId"></param>
    /// <param name="data"></param>
    /// <param name="payload"><see cref="string"/>/<see cref="byte"/>[]</param>
    public void Notify(string @event,
        string handlerId,
        string? data,
        object payload)
    {
        logger?.LogDebug("notify() [event:{E}]", @event);

        if (closed)
        {
            throw new InvalidAsynchronousStateException("PayloadChannel closed");
        }

        var notification = $"n:{@event}:{handlerId}:{data ?? "undefined"}";

        byte[] bytes = null!;

        if (notification.Length > MESSAGE_MAX_LEN)
        {
            throw new Exception("PayloadChannel notification too big");
        }
        else if (payload is string str
                     ? (bytes = Encoding.UTF8.GetBytes(str)).Length > MESSAGE_MAX_LEN
                     : payload is byte[] b && (bytes = b).Length > MESSAGE_MAX_LEN)
        {
            throw new Exception("PayloadChannel payload too big");
        }

        try
        {
            // This may throw if closed or remote side ended.
            var encoded = Encoding.UTF8.GetBytes(notification);
            producerSocket.Write(BitConverter.GetBytes(encoded.Length));
            producerSocket.Write(encoded);
        }
        catch (Exception error)
        {
            logger?.LogWarning("notify() | sending notification failed: {E}", error);

            return;
        }

        try
        {
            // This may throw if closed or remote side ended.
            producerSocket.Write(BitConverter.GetBytes(bytes.Length));
            producerSocket.Write(bytes);
        }
        catch (Exception error)
        {
            logger?.LogWarning("notify() | sending payload failed: {E}", error);
        }
    }

    public Task<object?> Request(string method,
        string handlerId,
        string data,
        object payload)
    {
        if (nextId < uint.MaxValue /*4294967295*/)
            ++nextId;
        else
            nextId = 1;
        var id = nextId;

        logger?.LogDebug("request() [{Method}, {Id}]", method, id);

        if (closed)
        {
            throw new InvalidStateError("PayloadChannel closed");
        }

        var request      = $"r:{id}:{method}:{handlerId}:{data.Serialize()}";
        var requestBytes = Encoding.UTF8.GetBytes(request);
        var payloadBytes = (payload as byte[])!;
        if (requestBytes.Length > MESSAGE_MAX_LEN)
        {
            throw new Exception("Channel request too big");
        }
        else if (payloadBytes.Length > MESSAGE_MAX_LEN)
        {
            throw new Exception("PayloadChannel payload too big");
        }

        // This may throw if closed or remote side ended.
        producerSocket.Write(BitConverter.GetBytes(requestBytes.Length));
        producerSocket.Write(requestBytes);
        producerSocket.Write(BitConverter.GetBytes(payloadBytes.Length));
        producerSocket.Write(payloadBytes);

        var ret = new TaskCompletionSource<object?>();
        Sent sent = new()
        {
            Id     = id,
            Method = method,
            Resolve = data2 =>
            {
                if (!sents.Remove(id))
                {
                    return;
                }

                ret.SetResult(data2);
            },
            Reject = error =>
            {
                if (!sents.Remove(id))
                {
                    return;
                }

                ret.SetException(error);
            },
            Close = () => { ret.SetException(new InvalidStateError("PayloadChannel closed")); }
        };

        // Add sent stuff to the map.
        sents[id] = sent;

        return ret.Task;
    }

    private readonly Action<ArraySegment<byte>> consumerSocketOnData;

    private readonly Action consumerSocketOnClosed;

    private readonly Action<Exception?> consumerSocketOnError;

    private readonly Action producerSocketOnClosed;

    private readonly Action<Exception?> producerSocketOnError;


    private void ProcessData(byte[] data)
    {
        if (ongoingNotification == null)
        {
            var jsonDocument = JsonDocument.Parse(Encoding.UTF8.GetString(data));
            var msg          = jsonDocument.RootElement;
            // If a response, retrieve its associated request.
            if (msg.TryGetProperty("id",out var idEle))
            {
                var id = idEle.GetUInt32();
                
                if (!sents.TryGetValue(id, out var sent))
                {
                    logger?.LogError("ProcessData() | Received response does not match any sent request [id:{Id}]", id);
                    return;
                }

                if (msg.TryGetProperty("accepted", out _))
                {
                    logger?.LogDebug("ProcessData() | Request succeed [method:{ValueMethod}, id:{ValueId}]", sent.Method, sent.Id);

                    sent.Resolve(msg.GetProperty("data").GetString());
                }
                else if (msg.TryGetProperty("error", out var errEle))
                {
                    var reason = msg.GetProperty("reason").GetString();
                    logger?.LogWarning("ProcessData() | Request failed [method:{ValueMethod}, id:{ValueId}]: {S}",
                        sent.Method, sent.Id, reason);

                    sent.Reject(new Exception(reason));
                    
                    switch (errEle.GetString())
                    {
                        case nameof(TypeError):
                            sent.Reject(new TypeError(reason));
                            break;

                        default:
                            sent.Reject(new Exception(reason));
                            break;
                    }
                }
                else
                {
                    logger?.LogError(
                        "ProcessData() | Received response is not accepted nor rejected [method:{ValueMethod}, id:{ValueId}]",
                        sent.Method, sent.Id);
                }
            }
            // If a notification emit it to the corresponding entity.
            else if (msg.TryGetProperty("targetId",out var targetIdEle) 
                     && !@msg.TryGetProperty("event",out var eventEle))
            {
                ongoingNotification = new OngoingNotification
                {
                    TargetId = targetIdEle.TryGetInt32(out var targetId)
                        ? targetId.ToString()
                        : targetIdEle.GetString()!,
                    Event = eventEle.GetString()!,
                    Data  = msg.GetProperty("data").GetString(),
                };
            }
            else
            {
                logger?.LogError(
                    "ProcessData() | Received data is not a notification nor a response");
            }
        }
        else
        {
            // Emit the corresponding event.
            _ = Emit(
                ongoingNotification.TargetId,
                ongoingNotification.Event,
                ongoingNotification.Data!,
                data);

            // Unset ongoing notification.
            ongoingNotification = null;
        }
    }
}