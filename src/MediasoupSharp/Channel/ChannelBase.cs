using System.Collections.Concurrent;
using System.Runtime.Serialization;
using System.Text.Json;
using MediasoupSharp.Exceptions;

namespace MediasoupSharp.Channel
{
    public abstract class ChannelBase : IChannel
    {
        #region Constants

        protected const int MessageMaxLen = PayloadMaxLen + sizeof(int);

        protected const int PayloadMaxLen = 1024 * 1024 * 4;

        #endregion Constants

        #region Protected Fields

        /// <summary>
        /// Logger
        /// </summary>
        protected readonly ILogger<ChannelBase> Logger;

        /// <summary>
        /// Closed flag.
        /// </summary>
        protected bool Closed;

        /// <summary>
        /// Close locker.
        /// </summary>
        protected readonly AsyncReaderWriterLock CloseLock = new();

        /// <summary>
        /// Worker id.
        /// </summary>
        protected readonly int WorkerId;

        /// <summary>
        /// Next id for messages sent to the worker process.
        /// </summary>
        protected uint NextId = 0;

        /// <summary>
        /// Map of pending sent requests.
        /// </summary>
        protected readonly ConcurrentDictionary<uint, Sent> Sents = new();

        #endregion Protected Fields

        #region Events

        public event Action<string, string, string?>? MessageEvent;

        #endregion Events

        public ChannelBase(ILogger<ChannelBase> logger, int workerId)
        {
            Logger = logger;
            WorkerId = workerId;
        }

        public async Task CloseAsync()
        {
            Logger.LogDebug($"CloseAsync() | Worker[{WorkerId}]");

            using (await CloseLock.WriteLockAsync())
            {
                if (Closed)
                {
                    return;
                }

                Closed = true;

                Cleanup();
            }
        }

        public virtual void Cleanup()
        {
            // Close every pending sent.
            try
            {
                Sents.Values.ForEach(m => m.Close());
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"Cleanup() | Worker[{WorkerId}] _sents.Values.ForEach(m => m.Close.Invoke())");
            }
        }

        private RequestMessage CreateRequestMessage(MethodId methodId, string? handlerId = null, object? data = null)
        {
            var id = InterlockedExtension.Increment(ref NextId);
            var method = methodId.GetDescription<EnumMemberAttribute>(x => x.Value);

            var requestMesssge = new RequestMessage
            {
                Id = id,
                Method = method,
                HandlerId = handlerId,
                Data = data?.Serialize(),
            };

            return requestMesssge;
        }

        protected abstract void SendRequestMessage(RequestMessage requestMessage, Sent sent);

        public async Task<string?> RequestAsync(string method, string? handlerId = null, object? data = null)
        {
            Logger.LogDebug($"RequestAsync() | Worker[{WorkerId}] Method:{""}");

            using (await CloseLock.ReadLockAsync())
            {
                if (Closed)
                {
                    throw new InvalidStateException("Channel closed");
                }

                var requestMessage = CreateRequestMessage(methodId, handlerId, data);

                var tcs = new TaskCompletionSource<string?>();
                var sent = new Sent
                {
                    RequestMessage = requestMessage,
                    Resolve = data =>
                    {
                        if (!Sents.TryRemove(requestMessage.Id!.Value, out _))
                        {
                            tcs.TrySetException(new Exception($"Received response does not match any sent request [id:{requestMessage.Id}]"));
                            return;
                        }
                        tcs.TrySetResult(data);
                    },
                    Reject = e =>
                    {
                        if (!Sents.TryRemove(requestMessage.Id!.Value, out _))
                        {
                            tcs.TrySetException(new Exception($"Received response does not match any sent request [id:{requestMessage.Id}]"));
                            return;
                        }
                        tcs.TrySetException(e);
                    },
                    Close = () =>
                    {
                        tcs.TrySetException(new InvalidStateException("Channel closed"));
                    },
                };
                if (!Sents.TryAdd(requestMessage.Id!.Value, sent))
                {
                    throw new Exception($"Error add sent request [id:{requestMessage.Id}]");
                }
                tcs.WithTimeout(TimeSpan.FromSeconds(15 + (0.1 * Sents.Count)), () => Sents.TryRemove(requestMessage.Id!.Value, out _));

                SendRequestMessage(requestMessage, sent);

                return await tcs.Task;
            }
        }

        #region Event handles

        public void ProcessMessage(string message)
        {
            try
            {
                // We can receive JSON messages (Channel messages) or log strings.
                var log = $"ProcessMessage() | Worker[{WorkerId}] payload: {message}";
                switch (message[0])
                {
                    // 123 = '{' (a Channel JSON messsage).
                    case '{':
                        ThreadPool.QueueUserWorkItem(_ =>
                        {
                            ProcessJson(message);
                        });
                        break;

                    // 68 = 'D' (a debug log).
                    case 'D':
                        if (!message.Contains("(trace)"))
                        {
                            Logger.LogDebug(log);
                        }

                        break;

                    // 87 = 'W' (a warn log).
                    case 'W':
                        if (!message.Contains("no suitable Producer"))
                        {
                            Logger.LogWarning(log);
                        }

                        break;

                    // 69 = 'E' (an error log).
                    case 'E':
                        Logger.LogError(log);
                        break;

                    // 88 = 'X' (a dump log).
                    case 'X':
                        Logger.LogDebug(log);
                        break;

                    default:
                        Logger.LogWarning($"ProcessMessage() | Worker[{WorkerId}] unexpected data, message: {message}");
                        break;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"ProcessMessage() | Worker[{WorkerId}] Received invalid message from the worker process, message: {message}");
                return;
            }
        }

        private void ProcessJson(string payload)
        {
            var jsonDocument = JsonDocument.Parse(payload);
            var msg = jsonDocument.RootElement;
            var id = msg.GetJsonElementOrNull("id")?.GetUInt32OrNull();
            var accepted = msg.GetJsonElementOrNull("accepted")?.GetBoolOrNull();
            // targetId 可能是 Number 或 String。不能使用 GetString()，否则可能报错：Cannot get the value of a token type 'Number' as a string"
            var targetId = msg.GetJsonElementOrNull("targetId")?.ToString();
            var @event = msg.GetJsonElementOrNull("event")?.GetString();
            var error = msg.GetJsonElementOrNull("error")?.GetString();
            var reason = msg.GetJsonElementOrNull("reason")?.GetString();
            var data = msg.GetJsonElementOrNull("data")?.ToString();

            // If a response, retrieve its associated request.
            if (id.HasValue && id.Value >= 0)
            {
                if (!Sents.TryGetValue(id.Value, out var sent))
                {
                    Logger.LogError($"ProcessMessage() | Worker[{WorkerId}] Received response does not match any sent request [id:{id}], payload:{payload}");
                    return;
                }

                if (accepted.HasValue && accepted.Value)
                {
                    Logger.LogDebug($"ProcessMessage() | Worker[{WorkerId}] Request succeed [method:{sent.RequestMessage.Method}, id:{sent.RequestMessage.Id}]");
                    sent.Resolve(data);
                }
                else if (!error.IsNullOrWhiteSpace())
                {
                    // 在 Node.js 实现中，error 的值可能是 "Error" 或 "TypeError"。
                    Logger.LogWarning($"ProcessMessage() | Worker[{WorkerId}] Request failed [method:{sent.RequestMessage.Method}, id:{sent.RequestMessage.Id}]: {reason}. payload:{payload}");

                    sent.Reject(new Exception($"Request failed [method:{sent.RequestMessage.Method}, id:{sent.RequestMessage.Id}]: {reason}. payload:{payload}"));
                }
                else
                {
                    Logger.LogError($"ProcessMessage() | Worker[{WorkerId}] Received response is not accepted nor rejected [method:{sent.RequestMessage.Method}, id:{sent.RequestMessage.Id}]. payload:{payload}");

                    sent.Reject(new Exception($"Received response is not accepted nor rejected [method:{sent.RequestMessage.Method}, id:{sent.RequestMessage.Id}]. payload:{payload}"));
                }
            }
            // If a notification emit it to the corresponding entity.
            else if (!targetId.IsNullOrWhiteSpace() && !@event.IsNullOrWhiteSpace())
            {
                MessageEvent?.Invoke(targetId!, @event!, data);
            }
            // Otherwise unexpected message.
            else
            {
                Logger.LogError($"ProcessMessage() | Worker[{WorkerId}] Received message is not a response nor a notification: {payload}");
            }
        }

        #endregion Event handles
    }
}
