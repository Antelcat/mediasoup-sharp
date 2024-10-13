using FlatBuffers.Message;
using FlatBuffers.Notification;
using FlatBuffers.Request;
using FlatBuffers.Response;
using Google.FlatBuffers;
using Microsoft.Extensions.ObjectPool;

namespace MediasoupSharp.Channel;

public interface IChannel
{
    event Action<string, Event, Notification>? OnNotification;

    ObjectPool<FlatBufferBuilder> BufferPool { get; }

    Task CloseAsync();

    Task<Response?> RequestAsync(FlatBufferBuilder bufferBuilder, Method method,
                                 global::FlatBuffers.Request.Body? bodyType = null, int? bodyOffset = null,
                                 string? handlerId = null);

    Task NotifyAsync(FlatBufferBuilder bufferBuilder, Event @event, global::FlatBuffers.Notification.Body? bodyType,
                     int? bodyOffset, string? handlerId);

    void ProcessMessage(Message message);
}
