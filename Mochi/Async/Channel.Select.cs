using System;
using System.Threading;

namespace Mochi.Async
{
    public static partial class Channel
    {
        public static ReadOnlyChannelMux<T1, T2> Mux<T1, T2>(ILowLevelReadOnlyChannel<T1> c1, ILowLevelReadOnlyChannel<T2> c2)
            => new ReadOnlyChannelMux<T1, T2>(c1, c2);

        public static ReadOnlyChannelMux<T1, T2, T3> Mux<T1, T2, T3>(ILowLevelReadOnlyChannel<T1> c1, ILowLevelReadOnlyChannel<T2> c2, ILowLevelReadOnlyChannel<T3> c3)
            => new ReadOnlyChannelMux<T1, T2, T3>(c1, c2, c3);

        public static ReadOnlyChannelMux<T1, T2, T3, T4> Mux<T1, T2, T3, T4>(ILowLevelReadOnlyChannel<T1> c1, ILowLevelReadOnlyChannel<T2> c2, ILowLevelReadOnlyChannel<T3> c3, ILowLevelReadOnlyChannel<T4> c4)
            => new ReadOnlyChannelMux<T1, T2, T3, T4>(c1, c2, c3, c4);

        public static Awaitable<ChannelSelectResult<T1, T2>> SelectAsync<T1, T2>(ILowLevelReadOnlyChannel<T1> c1, ILowLevelReadOnlyChannel<T2> c2, bool allowDefault, CancellationToken cancellationToken)
            => SelectAsync(Mux(c1, c2), allowDefault, new ChannelSelectResult<T1, T2>(-1), cancellationToken);

        public static Awaitable<ChannelSelectResult<T1, T2, T3>> SelectAsync<T1, T2, T3>(ILowLevelReadOnlyChannel<T1> c1, ILowLevelReadOnlyChannel<T2> c2, ILowLevelReadOnlyChannel<T3> c3, bool allowDefault, CancellationToken cancellationToken)
            => SelectAsync(Mux(c1, c2, c3), allowDefault, new ChannelSelectResult<T1, T2, T3>(-1, default, default, default), cancellationToken);

        public static Awaitable<ChannelSelectResult<T1, T2, T3, T4>> SelectAsync<T1, T2, T3, T4>(ILowLevelReadOnlyChannel<T1> c1, ILowLevelReadOnlyChannel<T2> c2, ILowLevelReadOnlyChannel<T3> c3, ILowLevelReadOnlyChannel<T4> c4, bool allowDefault, CancellationToken cancellationToken)
            => SelectAsync(Mux(c1, c2, c3, c4), allowDefault, new ChannelSelectResult<T1, T2, T3, T4>(-1, default, default, default, default), cancellationToken);

        private static Awaitable<TResult> SelectAsync<TCahnnel, TResult>(TCahnnel c, bool allowDefault, TResult defaultValue, CancellationToken cancellationToken)
            where TCahnnel : ILowLevelReadOnlyChannel<TResult>
        {
            if (cancellationToken.IsCancellationRequested)
            {
                var p = new Promise<TResult>();
                p.TrySetCanceled(cancellationToken);
                return new Awaitable<TResult>(p);
            }

            var invoked = 0;
            Func<bool> cas = () => Interlocked.CompareExchange(ref invoked, 1, 0) == 0;

            var promise = new Promise<TResult>();
            c.OnReceive(t =>
            {
                return !cancellationToken.IsCancellationRequested && cas() && promise.TrySetResult(t.value);
            });
            

            if (allowDefault && !promise.IsCompleted)
            {
                if (cas())
                {
                    promise.TrySetResult(defaultValue);
                }
            }
            else
            {
                if (cancellationToken.CanBeCanceled)
                {
                    var d = cancellationToken.Register(() =>
                    {
                        if (cas())
                        {
                            promise.TrySetCanceled(cancellationToken);
                        }
                    });
                    promise.AddContinuation(() => d.Dispose());
                }
            }

            return new Awaitable<TResult>(promise);
        }
    }
}
