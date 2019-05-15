using System;
using System.Threading;

namespace Mochi.Async
{
    public interface ILowLevelReadOnlyChannel<T>
    {
        void OnReceive(Func<(T value, bool ok), bool> accept);
    }

    public interface IReadOnlyChannel<T> : ILowLevelReadOnlyChannel<T>
    {
        Awaitable<(T value, bool ok)> ReceiveAsync(CancellationToken cancellationToken);
    }

    public interface IChannel<T> : IReadOnlyChannel<T>
    {
        Awaitable SendAsync(T value, CancellationToken cancellationToken);
    }
}
