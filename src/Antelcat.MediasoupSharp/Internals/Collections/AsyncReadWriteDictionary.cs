using Microsoft.VisualStudio.Threading;

namespace Antelcat.MediasoupSharp.Internals.Collections;

public class AsyncReadWriteDictionary<TKey, TValue> where TKey : class
{
    private readonly AsyncReaderWriterLock locker = new(null);

    public Dictionary<TKey, TValue> Raw { get; } = new();

    /// <summary>
    /// <see cref="AsyncReaderWriterLock.WriteLockAsync(CancellationToken)"/>
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public AsyncReaderWriterLock.Awaitable WriteLockAsync(CancellationToken cancellationToken = default) =>
        locker.WriteLockAsync(cancellationToken);

    /// <summary>
    /// <see cref="AsyncReaderWriterLock.ReadLockAsync"/>
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public AsyncReaderWriterLock.Awaitable ReadLockAsync(CancellationToken cancellationToken = default) =>
        locker.ReadLockAsync(cancellationToken);

    public async Task WriteAsync(Action<Dictionary<TKey, TValue>> action)
    {
        await using (await locker.WriteLockAsync()) action(Raw);
    }

    public async Task WriteAsync(Func<Dictionary<TKey, TValue>, Task> func)
    {
        await using (await locker.WriteLockAsync()) await func(Raw);
    }

    public async Task<T> ReadAsync<T>(Func<Dictionary<TKey, TValue>, Task<T>> func)
    {
        await using (await locker.ReadLockAsync())
        {
            return await func(Raw);
        }
    }
    
    public async Task<T> ReadAsync<T>(Func<Dictionary<TKey, TValue>, T> func)
    {
        await using (await locker.ReadLockAsync())
        {
            return func(Raw);
        }
    }
  
    public async Task ReadAsync(Func<Dictionary<TKey, TValue>, Task> func)
    {
        await using (await locker.ReadLockAsync())
        {
            await func(Raw);
        }
    }
    
    public async Task ReadAsync(Action<Dictionary<TKey, TValue>> action)
    {
        await using (await locker.ReadLockAsync())
        {
            action(Raw);
        }
    }
}