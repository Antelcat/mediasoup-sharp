namespace MediasoupSharp.PayloadChannel;

public interface IPayloadChannel
{
    event Action<string, string, string?, ArraySegment<byte>>? MessageEvent;

    Task CloseAsync();
    Task NotifyAsync(string @event, string handlerId, string? data, byte[] payload);
    Task<string?> RequestAsync(MethodId methodId, string handlerId, string data, byte[] payload);
    void Process(string message, byte[] payload);
}