using System.Runtime.InteropServices;
using FBS.Message;
using Google.FlatBuffers;
using Antelcat.MediasoupSharp.Internals.Extensions;
using Microsoft.Extensions.Logging;

namespace Antelcat.MediasoupSharp.Channel;

public class ChannelNative(ILogger<ChannelNative> logger, int workerId) : ChannelBase(logger, workerId)
{
    private readonly OutgoingMessageBuffer<RequestMessage> requestMessageQueue = new();

    protected override void SendRequest(Sent sent)
    {
        requestMessageQueue.Queue.Enqueue(sent.RequestMessage);

        try
        {
            // Notify worker that there is something to read
            if(LibMediasoupWorkerNative.uv_async_send(requestMessageQueue.Handle) != 0)
            {
                Logger.LogError("uv_async_send call failed");
                sent.Reject(new Exception("uv_async_send call failed"));
            }
        }
        catch(Exception ex)
        {
            Logger.LogError(ex, "uv_async_send call failed");
            sent.Reject(ex);
        }
    }

    protected override void SendNotification(RequestMessage requestMessage)
    {
        requestMessageQueue.Queue.Enqueue(requestMessage);

        try
        {
            // Notify worker that there is something to read
            if(LibMediasoupWorkerNative.uv_async_send(requestMessageQueue.Handle) != 0)
            {
                Logger.LogError("uv_async_send call failed");
            }
        }
        catch(Exception ex)
        {
            Logger.LogError(ex, "uv_async_send call failed");
        }
    }

    private RequestMessage? ProduceMessage(IntPtr message, IntPtr messageLen, IntPtr messageCtx, IntPtr handle)
    {
        requestMessageQueue.Handle = handle;

        if(!requestMessageQueue.Queue.TryDequeue(out var requestMessage))
        {
            return null;
        }

        if(requestMessage!.Payload.Count == 0)
        {
            throw new Exception("Channel request failed. Zero length.");
        }

        if(requestMessage!.Payload.Count > MessageMaxLen)
        {
            throw new Exception("Channel request failed. Invalid length.");
        }

        var messageBytesHandle = GCHandle.Alloc(requestMessage!.Payload.Array, GCHandleType.Pinned);
        //var messagePtr = (IntPtr)(messageBytesHandle.AddrOfPinnedObject().ToInt64() + requestMessage!.Payload.Offset);
        //var messagePtr = Marshal.UnsafeAddrOfPinnedArrayElement(requestMessage!.Payload.Array!, 0);

        var messagePtr = Marshal.UnsafeAddrOfPinnedArrayElement(requestMessage!.Payload.Array!, requestMessage!.Payload.Offset);

        // 将数据的指针写入 message
        var temp1 = messagePtr.ToBytes(); // 4 or 8 bytes
        Marshal.Copy(temp1, 0, message, temp1.Length);

        // 将数据的长度写入 messageLen
        //var temp2 = BitConverter.GetBytes(requestMessage!.Payload.Array!.Length); // 4 bytes
        var temp2 = BitConverter.GetBytes(requestMessage!.Payload.Count); // 4 bytes
        Marshal.Copy(temp2, 0, messageLen, requestMessage!.Payload.Count);

        // 将消息句柄写入 messageCtx，以便 OnChannelReadFree 释放。
        var temp3 = GCHandle.ToIntPtr(messageBytesHandle).ToBytes();  // 4 or 8 bytes
        Marshal.Copy(temp3, 0, messageCtx, temp3.Length);
        return requestMessage;
    }

    #region P/Invoke Channel

    internal static readonly LibMediasoupWorkerNative.ChannelReadFreeFn OnChannelReadFree = (
        _, // message
        _, // messageLen
        messageCtx
    ) =>
    {
        var messageBytesHandle = GCHandle.FromIntPtr(messageCtx);
        messageBytesHandle.Free();
    };

    internal static readonly LibMediasoupWorkerNative.ChannelReadFn OnChannelRead = (
        message,
        messageLen,
        messageCtx,
        handle,
        ctx
    ) =>
    {
        var channel        = (ChannelNative)GCHandle.FromIntPtr(ctx).Target!;
        var requestMessage = channel.ProduceMessage(message, messageLen, messageCtx, handle);
        return requestMessage == null ? null : OnChannelReadFree;
    };

    internal static readonly LibMediasoupWorkerNative.ChannelWriteFn OnChannelWrite = (payload, payloadLen, channelWriteCtx) =>
    {
        // 因为 ProcessMessage 会在子线程处理数据，故拷贝一份。
        var messageBytes = new byte[payloadLen];
        Marshal.Copy(payload, messageBytes, 0, (int)payloadLen);
        var byteBuffer = new ByteBuffer(messageBytes);
        var fbsMessage = Message.GetRootAsMessage(byteBuffer);

        var channel = (IChannel)GCHandle.FromIntPtr(channelWriteCtx).Target!;
        channel.ProcessMessage(fbsMessage);
    };

    #endregion
}