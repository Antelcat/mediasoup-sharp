using System.Text;
using System.Text.Json;
using MediasoupSharp.Channel;
using Microsoft.Extensions.Logging;

namespace MediasoupSharp.PayloadChannel
{
    public class PayloadChannel : EnhancedEventEmitter
    {
        #region Constants

        private const int RecvBufferMaxLen = PayloadMaxLen * 2;

        #endregion Constants

        #region Protected Fields

        /// <summary>
        /// Unix Socket instance for sending messages to the worker process.
        /// </summary>
        private readonly UVStream producerSocket;

        /// <summary>
        /// Unix Socket instance for receiving messages to the worker process.
        /// </summary>
        private readonly UVStream consumerSocket;

        #endregion Protected Fields

        #region Private Fields

        /// <summary>
        /// Buffer for reading messages from the worker.
        /// </summary>
        private readonly byte[] recvBuffer;
        private int recvBufferCount;

        #endregion

        #region Events

        public override event Action<string, string, string?, ArraySegment<byte>>? MessageEvent;

        #endregion Events

        public PayloadChannel(ILogger<PayloadChannel> logger, UVStream producerSocket, UVStream consumerSocket, int processId) : base(logger, processId)
        {
            this.producerSocket = producerSocket;
            this.consumerSocket = consumerSocket;

            recvBuffer = new byte[RecvBufferMaxLen];
            recvBufferCount = 0;

            this.consumerSocket.Data += ConsumerSocketOnData;
            this.consumerSocket.Closed += ConsumerSocketOnClosed;
            this.consumerSocket.Error += ConsumerSocketOnError;
            this.producerSocket.Closed += ProducerSocketOnClosed;
            this.producerSocket.Error += ProducerSocketOnError;
        }

        public override void Cleanup()
        {
            base.Cleanup();

            // Remove event listeners but leave a fake 'error' hander to avoid
            // propagation.
            consumerSocket.Closed -= ConsumerSocketOnClosed;
            consumerSocket.Error -= ConsumerSocketOnError;

            producerSocket.Closed -= ProducerSocketOnClosed;
            producerSocket.Error -= ProducerSocketOnError;

            // Destroy the socket after a while to allow pending incoming messages.
            // 在 Node.js 实现中，延迟了 200 ms。
            try
            {
                producerSocket.Close();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"CloseAsync() | Worker[{WorkerId}]");
            }

            try
            {
                consumerSocket.Close();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"CloseAsync() | Worker[{WorkerId}]");
            }
        }

        protected override void SendNotification(RequestMessage notification)
        {
            var messageString = $"n:{notification.Event}:{notification.HandlerId}:{notification.Data ?? "undefined"}";
            var messageBytes = Encoding.UTF8.GetBytes(messageString);
            var payloadBytes = notification.Payload!;

            Loop.Default.Sync(() =>
            {
                try
                {
                    var messageBytesLengthBytes = BitConverter.GetBytes(messageBytes.Length);

                    // This may throw if closed or remote side ended.
                    producerSocket.Write(messageBytesLengthBytes, ex =>
                    {
                        if (ex != null)
                        {
                            Logger.LogError(ex, $"_producerSocket.Write() | Worker[{WorkerId}] Error");
                        }
                    });
                    producerSocket.Write(messageBytes, ex =>
                    {
                        if (ex != null)
                        {
                            Logger.LogError(ex, $"_producerSocket.Write() | Worker[{WorkerId}] Error");
                        }
                    });
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, $"NotifyAsync() | Worker[{WorkerId}] Sending notification failed");
                    return;
                }

                try
                {
                    var payloadBytesLengthBytes = BitConverter.GetBytes(payloadBytes.Length);

                    // This may throw if closed or remote side ended.
                    producerSocket.Write(payloadBytesLengthBytes, ex =>
                    {
                        if (ex != null)
                        {
                            Logger.LogError(ex, $"_producerSocket.Write() | Worker[{WorkerId}] Error");
                        }
                    });
                    producerSocket.Write(payloadBytes, ex =>
                    {
                        if (ex != null)
                        {
                            Logger.LogError(ex, $"_producerSocket.Write() | Worker[{WorkerId}] Error");
                        }
                    });
                }
                catch (Exception ex)
                {
                    Logger.LogWarning(ex, $"NotifyAsync() | Worker[{WorkerId}] Sending notification failed");
                    return;
                }
            });
        }

        protected override void SendRequestMessage(RequestMessage requestMessage, Sent sent)
        {
            var requestMessageJson = $"r:{requestMessage.Id}:{requestMessage.Method}:{requestMessage.HandlerId}:{requestMessage.Data ?? "undefined"}";
            var requestMessageBytes = Encoding.UTF8.GetBytes(requestMessageJson);

            if (requestMessageBytes.Length > MessageMaxLen)
            {
                throw new Exception("PayloadChannel message too big");
            }
            else if (requestMessage.Payload != null && requestMessage.Payload.Length > PayloadMaxLen)
            {
                throw new Exception("PayloadChannel payload too big");
            }

            Loop.Default.Sync(() =>
            {
                try
                {
                    var requestMessageBytesLengthBytes = BitConverter.GetBytes(requestMessageBytes.Length);

                    // This may throw if closed or remote side ended.
                    producerSocket.Write(requestMessageBytesLengthBytes, ex =>
                    {
                        if (ex != null)
                        {
                            Logger.LogError(ex, $"_producerSocket.Write() | Worker[{WorkerId}] Error");
                            sent.Reject(ex);
                        }
                    });
                    // This may throw if closed or remote side ended.
                    producerSocket.Write(requestMessageBytes, ex =>
                    {
                        if (ex != null)
                        {
                            Logger.LogError(ex, $"_producerSocket.Write() | Worker[{WorkerId}] Error");
                            sent.Reject(ex);
                        }
                    });

                    if (requestMessage.Payload != null)
                    {
                        var payloadLengthBytes = BitConverter.GetBytes(requestMessage.Payload.Length);

                        // This may throw if closed or remote side ended.
                        producerSocket.Write(payloadLengthBytes, ex =>
                        {
                            if (ex != null)
                            {
                                Logger.LogError(ex, $"_producerSocket.Write() | Worker[{WorkerId}] Error");
                                sent.Reject(ex);
                            }
                        });

                        // This may throw if closed or remote side ended.
                        producerSocket.Write(requestMessage.Payload, ex =>
                        {
                            if (ex != null)
                            {
                                Logger.LogError(ex, $"_producerSocket.Write() | Worker[{WorkerId}] Error");
                                sent.Reject(ex);
                            }
                        });
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, $"_producerSocket.Write() | Worker[{WorkerId}] Error");
                    sent.Reject(ex);
                }
            });
        }

        #region Event handles

        private void ConsumerSocketOnData(ArraySegment<byte> data)
        {
            try
            {
                var readCount = 0;
                while (readCount < recvBufferCount - sizeof(int) - 1)
                {
                    var msgLen = BitConverter.ToInt32(recvBuffer, readCount);
                    readCount += sizeof(int);
                    if (readCount >= recvBufferCount)
                    {
                        // Incomplete data.
                        break;
                    }

                    var payload = new byte[msgLen];
                    Array.Copy(recvBuffer, readCount, payload, 0, msgLen);
                    readCount += msgLen;

                    ThreadPool.QueueUserWorkItem(_ =>
                    {
                        Process(payload);
                    });
                }

                var remainingLength = recvBufferCount - readCount;
                if (remainingLength == 0)
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
            catch (Exception ex)
            {
                Logger.LogError(ex, $"ConsumerSocketOnData() | Worker[{WorkerId}] Invalid data received from the worker process.");
                return;
            }
        }

        private void ConsumerSocketOnClosed()
        {
            Logger.LogDebug($"ConsumerSocketOnClosed() | Worker[{WorkerId}] Consumer Channel ended by the worker process");
        }

        private void ConsumerSocketOnError(Exception? exception)
        {
            Logger.LogDebug(exception, $"ConsumerSocketOnError() | Worker[{WorkerId}] Consumer Channel error");
        }

        private void ProducerSocketOnClosed()
        {
            Logger.LogDebug($"ProducerSocketOnClosed() | Worker[{WorkerId}] Producer Channel ended by the worker process");
        }

        private void ProducerSocketOnError(Exception? exception)
        {
            Logger.LogDebug(exception, $"ProducerSocketOnError() | Worker[{WorkerId}] Producer Channel error");
        }

        #endregion Event handles

        #region Process Methods

        public override void Process(string message, byte[] payload)
        {
            throw new NotImplementedException();
        }

        private void Process(byte[] payload)
        {
            if (OngoingNotification == null)
            {
                var message = Encoding.UTF8.GetString(payload, 0, payload.Length);
                var jsonDocument = JsonDocument.Parse(message);
                var msg = jsonDocument.RootElement;
                var id = msg.GetJsonElementOrNull("id")?.GetUInt32OrNull();
                var accepted = msg.GetJsonElementOrNull("accepted")?.GetBoolOrNull();
                // targetId 可能是 Number 或 String。不能使用 GetString()，否则可能报错：Cannot get the value of a token type 'Number' as a string"
                var targetId = msg.GetJsonElementOrNull("targetId")?.ToString();
                var @event = msg.GetJsonElementOrNull("event")?.GetString();
                var error = msg.GetJsonElementOrNull("error")?.GetString();
                var reason = msg.GetJsonElementOrNull("reason")?.GetString();
                var data = msg.GetJsonElementOrNull("data")?.GetString();

                // If a response, retrieve its associated request.
                if (id.HasValue && id.Value >= 0)
                {
                    if (!Sents.TryGetValue(id.Value, out var sent))
                    {
                        Logger.LogError($"ProcessData() | Worker[{WorkerId}] Received response does not match any sent request [id:{id}]");
                        return;
                    }

                    if (accepted.HasValue && accepted.Value)
                    {
                        Logger.LogDebug($"ProcessData() | Worker[{WorkerId}] Request succeed [method:{sent.RequestMessage.Method}, id:{sent.RequestMessage.Id}]");

                        sent.Resolve(data);
                    }
                    else if (!error.IsNullOrWhiteSpace())
                    {
                        // 在 Node.js 实现中，error 的值可能是 "Error" 或 "TypeError"。
                        Logger.LogWarning($"ProcessData() | Worker[{WorkerId}] Request failed [method:{sent.RequestMessage.Method}, id:{sent.RequestMessage.Id}]: {reason}");

                        sent.Reject(new Exception(reason));
                    }
                    else
                    {
                        Logger.LogError($"ProcessData() | Worker[{WorkerId}] Received response is not accepted nor rejected [method:{sent.RequestMessage.Method}, id:{sent.RequestMessage.Id}]");
                    }
                }
                // If a notification emit it to the corresponding entity.
                else if (!targetId.IsNullOrWhiteSpace() && !@event.IsNullOrWhiteSpace())
                {
                    OngoingNotification = new OngoingNotification
                    {
                        TargetId = targetId!,
                        Event = @event!,
                        Data = data,
                    };
                }
                else
                {
                    Logger.LogError($"ProcessData() | Worker[{WorkerId}] Received data is not a notification nor a response");
                    return;
                }
            }
            else
            {
                // Emit the corresponding event.
                MessageEvent?.Invoke(OngoingNotification.TargetId, OngoingNotification.Event, OngoingNotification.Data, new ArraySegment<byte>(payload));

                // Unset ongoing notification.
                OngoingNotification = null;
            }
        }

        #endregion Private Methods
    }
}
