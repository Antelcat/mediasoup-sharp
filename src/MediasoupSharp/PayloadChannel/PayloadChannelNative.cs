using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using MediasoupSharp.Channel;

namespace MediasoupSharp.PayloadChannel;

public class PayloadChannelNative : PayloadChannelBase
{
    #region Events

    public override event Action<string, string, string?, ArraySegment<byte>>? MessageEvent;

    #endregion Events

    private readonly OutgoingMessageBuffer<RequestMessage> requestMessageQueue = new();

    public PayloadChannelNative(ILogger<PayloadChannelNative> logger, int workerId):base(logger, workerId)
    {

    }

    protected override void SendNotification(RequestMessage notification)
    {
        requestMessageQueue.Queue.Enqueue(notification);

        try
        {
            // Notify worker that there is something to read
            if (LibMediasoupWorkerNative.uv_async_send(requestMessageQueue.Handle) != 0)
            {
                Logger.LogError("uv_async_send call failed");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "uv_async_send call failed");
        }
    }

    protected override void SendRequestMessage(RequestMessage requestMessage, Sent sent)
    {
        requestMessageQueue.Queue.Enqueue(requestMessage);

        try
        {
            // Notify worker that there is something to read
            if (LibMediasoupWorkerNative.uv_async_send(requestMessageQueue.Handle) != 0)
            {
                Logger.LogError("uv_async_send call failed");
                sent.Reject(new Exception("uv_async_send call failed"));
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "uv_async_send call failed");
            sent.Reject(ex);
        }
    }

    private RequestMessage? ProduceMessage(IntPtr message, IntPtr messageLen, IntPtr messageCtx,
        IntPtr payload, IntPtr payloadLen, IntPtr payloadCapacity,
        IntPtr handle)
    {
        requestMessageQueue.Handle = handle;

        if (!requestMessageQueue.Queue.TryDequeue(out var requestMessage))
        {
            return null;
        }

        string requestMessageString;
        if (requestMessage.Event.IsNullOrWhiteSpace())
        {
            requestMessageString = $"n:{requestMessage.Event}:{requestMessage.HandlerId}:{requestMessage.Data ?? "undefined"}";
        }
        else
        {
            requestMessageString = $"r:{requestMessage.Id}:{requestMessage.Method}:{requestMessage.HandlerId}:{requestMessage.Data ?? "undefined"}";
        }
        var requestMessageBytes = Encoding.UTF8.GetBytes(requestMessageString);

        if (requestMessageBytes.Length > MessageMaxLen)
        {
            throw new Exception("PayloadChannel message too big");
        }
        else if (requestMessage.Payload != null && requestMessage.Payload.Length > PayloadMaxLen)
        {
            throw new Exception("PayloadChannel payload too big");
        }

        // message
        var messageBytesHandle = GCHandle.Alloc(requestMessageBytes, GCHandleType.Pinned);
        var messagePtr = Marshal.UnsafeAddrOfPinnedArrayElement(requestMessageBytes, 0);
        var temp = messagePtr.ToBytes();
        Marshal.Copy(temp, 0, message, temp.Length);

        // messageLen
        temp = BitConverter.GetBytes(requestMessageBytes.Length);
        Marshal.Copy(temp, 0, messageLen, temp.Length);

        // messageCtx
        temp = GCHandle.ToIntPtr(messageBytesHandle).ToBytes();
        Marshal.Copy(temp, 0, messageCtx, temp.Length);

        // payload
        if (requestMessage.Payload != null)
        {
            // payload
            var payloadBytesHandle = GCHandle.Alloc(requestMessage.Payload, GCHandleType.Pinned);
            var payloadBytesPtr = Marshal.UnsafeAddrOfPinnedArrayElement(requestMessage.Payload, 0);
            temp = payloadBytesPtr.ToBytes();
            Marshal.Copy(temp, 0, payload, temp.Length);

            // payloadLen
            temp = BitConverter.GetBytes(requestMessage.Payload.Length);
            Marshal.Copy(temp, 0, payloadLen, temp.Length);

            // payloadCapacity
            temp = GCHandle.ToIntPtr(payloadBytesHandle).ToBytes();
            Marshal.Copy(temp, 0, payloadCapacity, temp.Length);
        }
        else
        {
            // payload
            temp = IntPtr.Zero.ToBytes();
            Marshal.Copy(temp, 0, payload, temp.Length);

            // payloadLen
            temp = BitConverter.GetBytes(0);
            Marshal.Copy(temp, 0, payloadLen, temp.Length);

            // payloadCapacity
            temp = IntPtr.Zero.ToBytes();
            Marshal.Copy(temp, 0, payloadCapacity, temp.Length);
        }

        return requestMessage;
    }

    public override void Process(string message, byte[] payload)
    {
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

        if (OngoingNotification != null)
        {
            // Emit the corresponding event.
            MessageEvent?.Invoke(OngoingNotification.TargetId, OngoingNotification.Event, OngoingNotification.Data, new ArraySegment<byte>(payload));

            // Unset ongoing notification.
            OngoingNotification = null;
        }
    }

    #region P/Invoke PayloadChannel

    internal static readonly LibMediasoupWorkerNative.PayloadChannelReadFreeFn OnPayloadChannelReadFree = (message, messageLen, messageCtx) =>
    {
        if (messageLen != 0)
        {
            var messageBytesHandle = GCHandle.FromIntPtr(messageCtx);
            messageBytesHandle.Free();
        }
    };

    internal static readonly LibMediasoupWorkerNative.PayloadChannelReadFn OnPayloadChannelRead = (message, messageLen, messageCtx,
        payload, payloadLen, payloadCapacity,
        handle, ctx) =>
    {
        var payloadChannel = (PayloadChannelNative)GCHandle.FromIntPtr(ctx).Target!;
        var requestMessage = payloadChannel.ProduceMessage(message, messageLen, messageCtx,
            payload, payloadLen, payloadCapacity,
            handle);
        return requestMessage == null ? null : OnPayloadChannelReadFree;
    };

    internal static readonly LibMediasoupWorkerNative.PayloadChannelWriteFn OnPayloadchannelWrite = (message, messageLen, payload, payloadLen, ctx) =>
    {
        var handle = GCHandle.FromIntPtr(ctx);
        var payloadChannel = (IPayloadChannel)handle.Target!;

        var payloadBytes = new byte[payloadLen];
        Marshal.Copy(payload, payloadBytes, 0, (int)payloadLen);
        payloadChannel.Process(message, payloadBytes);
    };

    #endregion
}