using System.Runtime.CompilerServices;

namespace NCoreUtils.EmailSender.Dispatchers.Stalwart;

internal sealed class AsyncLock
    : IDisposable
{
    [method: MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly struct Releaser(AsyncLock host)
        : IDisposable
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose() => host._semaphore.Release();
    }

    private readonly SemaphoreSlim _semaphore = new(1, 1);

    private InterlockedBoolean IsDisposed;

    public async ValueTask<Releaser> LockAsync(CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        return new(this);
    }

    public void Dispose()
    {
        if (IsDisposed.TrySet())
        {
            _semaphore.Dispose();
        }
    }
}