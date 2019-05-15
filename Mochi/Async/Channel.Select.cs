using System;
using System.Threading;

namespace Mochi.Async
{
    public readonly struct ChannelSelectResult<T>
    {
        public readonly bool IsActive; 
        public readonly T Result;
        public readonly bool Ok;

        public ChannelSelectResult(T result, bool ok)
            => (this.IsActive, this.Result, this.Ok) = (true, result, ok);
    }

    public readonly struct ChannelSelectResult<T1, T2>
    {
        public readonly int Index;
        public readonly ChannelSelectResult<T1> Item1;
        public readonly ChannelSelectResult<T2> Item2;

        public ChannelSelectResult(int index)
        {
            this.Index = index;
            this.Item1 = default;
            this.Item2 = default;
        }

        public ChannelSelectResult(ChannelSelectResult<T1> item1)
        {
            this.Index = -1;
            this.Item1 = item1;
            this.Item2 = default;
        }

        public ChannelSelectResult(ChannelSelectResult<T2> item2)
        {
            this.Index = -1;
            this.Item1 = default;
            this.Item2 = item2;
        }

        public void Deconstruct(ref ChannelSelectResult<T1> item1, ref ChannelSelectResult<T2> item2)
        {
            item1 = this.Item1;
            item2 = this.Item2;
        }
    }

    public readonly struct ChannelSelectResult<T1, T2, T3>
    {
        public readonly int Index;
        public readonly ChannelSelectResult<T1> Item1;
        public readonly ChannelSelectResult<T2> Item2;
        public readonly ChannelSelectResult<T3> Item3;

        public ChannelSelectResult(int index, ChannelSelectResult<T1> item1, ChannelSelectResult<T2> item2, ChannelSelectResult<T3> item3)
        {
            this.Index = index;
            this.Item1 = item1;
            this.Item2 = item2;
            this.Item3 = item3;
        }

        public void Deconstruct(ref ChannelSelectResult<T1> item1, ref ChannelSelectResult<T2> item2, ref ChannelSelectResult<T3> item3)
        {
            item1 = this.Item1;
            item2 = this.Item2;
            item3 = this.Item3;
        }
    }

    public readonly struct ChannelSelectResult<T1, T2, T3, T4>
    {
        public readonly int Index;
        public readonly ChannelSelectResult<T1> Item1;
        public readonly ChannelSelectResult<T2> Item2;
        public readonly ChannelSelectResult<T3> Item3;
        public readonly ChannelSelectResult<T4> Item4;

        public ChannelSelectResult(int index, ChannelSelectResult<T1> item1, ChannelSelectResult<T2> item2, ChannelSelectResult<T3> item3, ChannelSelectResult<T4> item4)
        {
            this.Index = index;
            this.Item1 = item1;
            this.Item2 = item2;
            this.Item3 = item3;
            this.Item4 = item4;
        }

        public void Deconstruct(ref ChannelSelectResult<T1> item1, ref ChannelSelectResult<T2> item2, ref ChannelSelectResult<T3> item3, ref ChannelSelectResult<T4> item4)
        {
            item1 = this.Item1;
            item2 = this.Item2;
            item3 = this.Item3;
            item4 = this.Item4;
        }
    }

    public static partial class Channel
    {
        public static Awaitable<ChannelSelectResult<T1, T2>> SelectAsync<T1, T2>(IReadOnlyChannel<T1> channel1, IReadOnlyChannel<T2> channel2, bool allowDefault, CancellationToken cancellationToken)
        {
            var mux = new ReadOnlyChannelMux<T1, T2>(new object(), channel1, channel2);
            return SelectAsync(mux, allowDefault, cancellationToken);
        }

        public static Awaitable<ChannelSelectResult<T1, T2, T3>> SelectAsync<T1, T2, T3>(IReadOnlyChannel<T1> channel1, IReadOnlyChannel<T2> channel2, IReadOnlyChannel<T3> channel3, bool allowDefault, CancellationToken cancellationToken)
        {
            var sync = new object();
            var mux1 = new ReadOnlyChannelMux<T1, T2>(sync, channel1, channel2);
            var mux2 = new ReadOnlyChannelMux<ChannelSelectResult<T1, T2>, T3>(sync, mux1, channel3);
            return SelectAsync(mux2, allowDefault, cancellationToken)
                .Select(t =>
                {
                    var r1 = t.Item1.Result.Item1;
                    var r2 = t.Item1.Result.Item2;
                    var r3 = t.Item2;
                    int index;
                    if (r1.IsActive) index = 0;
                    else if (r2.IsActive) index = 1;
                    else if (r3.IsActive) index = 2;
                    else index = -1;

                    return new ChannelSelectResult<T1, T2, T3>(index, r1, r2, r3);
                });
        }

        public static Awaitable<ChannelSelectResult<T1, T2, T3, T4>> SelectAsync<T1, T2, T3, T4>(IReadOnlyChannel<T1> channel1, IReadOnlyChannel<T2> channel2, IReadOnlyChannel<T3> channel3, IReadOnlyChannel<T4> channel4, bool allowDefault, CancellationToken cancellationToken)
        {
            var sync = new object();
            var mux1 = new ReadOnlyChannelMux<T1, T2>(sync, channel1, channel2);
            var mux2 = new ReadOnlyChannelMux<ChannelSelectResult<T1, T2>, T3>(sync, mux1, channel3);
            var mux3 = new ReadOnlyChannelMux<ChannelSelectResult<ChannelSelectResult<T1, T2>, T3>, T4>(sync, mux2, channel4);
            return SelectAsync(mux3, allowDefault, cancellationToken)
                .Select(t =>
                {
                    var r1 = t.Item1.Result.Item1.Result.Item1;
                    var r2 = t.Item1.Result.Item1.Result.Item2;
                    var r3 = t.Item1.Result.Item2;
                    var r4 = t.Item2;
                    int index;
                    if (r1.IsActive) index = 0;
                    else if (r2.IsActive) index = 1;
                    else if (r3.IsActive) index = 2;
                    else if (r4.IsActive) index = 3;
                    else index = -1;

                    return new ChannelSelectResult<T1, T2, T3, T4>(index, r1, r2, r3, r4);
                });
        }

        public static Awaitable<ChannelSelectResult<T1, T2>> SelectAsync<T1, T2>(ReadOnlyChannelMux<T1, T2> mux, bool allowDefault, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                var p = new Promise<ChannelSelectResult<T1, T2>>();
                p.TrySetCanceled(cancellationToken);
                return new Awaitable<ChannelSelectResult<T1, T2>>(p);
            }

            lock (mux.Sync)
            {
                var promise = new Promise<ChannelSelectResult<T1, T2>>();
                mux.OnReceive(t =>
                {
                    return !cancellationToken.IsCancellationRequested && promise.TrySetResult(t.value);
                });

                if (allowDefault && !promise.IsCompleted)
                {
                    promise.TrySetResult(new ChannelSelectResult<T1, T2>(-1));
                }
                else
                {
                    if (cancellationToken.CanBeCanceled)
                    {
                        var d = cancellationToken.Register(() => promise.TrySetCanceled(cancellationToken));
                        promise.AddContinuation(() => d.Dispose());
                    }
                }

                return new Awaitable<ChannelSelectResult<T1, T2>>(promise);
            }
        }
    }

    public struct ReadOnlyChannelMux<T1, T2> : ILowLevelReadOnlyChannel<ChannelSelectResult<T1, T2>>
    {
        public object Sync { get; }
        private ILowLevelReadOnlyChannel<T1> channel1;
        private ILowLevelReadOnlyChannel<T2> channel2;

        public ReadOnlyChannelMux(object sync, ILowLevelReadOnlyChannel<T1> channel1, ILowLevelReadOnlyChannel<T2> channel2)
        {
            this.Sync = sync;
            this.channel1 = channel1;
            this.channel2 = channel2;
        }

        public void OnReceive(Func<(ChannelSelectResult<T1, T2> value, bool ok), bool> accept)
        {
            var _sync = this.Sync;
            var received = false;
            this.channel1.OnReceive(t =>
            {
                lock (_sync)
                {
                    if (received) return false;
                    received = true;

                    return accept((new ChannelSelectResult<T1, T2>(new ChannelSelectResult<T1>(t.value, t.ok)), true));
                }
            });

            this.channel2.OnReceive(t =>
            {
                lock (_sync)
                {
                    if (received) return false;
                    received = true;

                    return accept((new ChannelSelectResult<T1, T2>(new ChannelSelectResult<T2>(t.value, t.ok)), true));
                }
            });
        }
    }
}
