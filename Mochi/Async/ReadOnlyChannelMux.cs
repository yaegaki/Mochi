using System;

namespace Mochi.Async
{
    public static class ChannelSelectResult
    {
        public static ChannelSelectResult<T> Create<T>((T result, bool ok) t)
            => new ChannelSelectResult<T>(t.result, t.ok);
    }

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
            this.Index = 0;
            this.Item1 = item1;
            this.Item2 = default;
        }

        public ChannelSelectResult(ChannelSelectResult<T2> item2)
        {
            this.Index = 1;
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
    public struct ReadOnlyChannelMux<T1, T2> : ILowLevelReadOnlyChannel<ChannelSelectResult<T1, T2>>
    {
        private ILowLevelReadOnlyChannel<T1> channel1;
        private ILowLevelReadOnlyChannel<T2> channel2;

        public ReadOnlyChannelMux(ILowLevelReadOnlyChannel<T1> channel1, ILowLevelReadOnlyChannel<T2> channel2)
        {
            this.channel1 = channel1;
            this.channel2 = channel2;
        }

        public void OnReceive(Func<(ChannelSelectResult<T1, T2> value, bool ok), bool> accept)
        {
            this.channel1.OnReceive(t =>
            {
                return accept((new ChannelSelectResult<T1, T2>(ChannelSelectResult.Create(t)), true));
            });

            this.channel2.OnReceive(t =>
            {
                return accept((new ChannelSelectResult<T1, T2>(ChannelSelectResult.Create(t)), true));
            });
        }
    }

    public struct ReadOnlyChannelMux<T1, T2, T3> : ILowLevelReadOnlyChannel<ChannelSelectResult<T1, T2, T3>>
    {
        private ReadOnlyChannelMux<T1, T2> mux;
        private ILowLevelReadOnlyChannel<T3> last;

        public ReadOnlyChannelMux(ILowLevelReadOnlyChannel<T1> c1, ILowLevelReadOnlyChannel<T2> c2, ILowLevelReadOnlyChannel<T3> c3)
        {
            this.mux = new ReadOnlyChannelMux<T1, T2>(c1, c2);
            this.last = c3;
        }

        public void OnReceive(Func<(ChannelSelectResult<T1, T2, T3> value, bool ok), bool> accept)
        {
            this.mux.OnReceive(t =>
            {
                return accept((new ChannelSelectResult<T1, T2, T3>(t.value.Index, t.value.Item1, t.value.Item2, default), true));
            });

            this.last.OnReceive(t =>
            {
                return accept((new ChannelSelectResult<T1, T2, T3>(2, default, default, ChannelSelectResult.Create(t)), true));
            });
        }
    }

    public struct ReadOnlyChannelMux<T1, T2, T3, T4> : ILowLevelReadOnlyChannel<ChannelSelectResult<T1, T2, T3, T4>>
    {
        private ReadOnlyChannelMux<T1, T2, T3> mux;
        private ILowLevelReadOnlyChannel<T4> last;

        public ReadOnlyChannelMux(ILowLevelReadOnlyChannel<T1> c1, ILowLevelReadOnlyChannel<T2> c2, ILowLevelReadOnlyChannel<T3> c3, ILowLevelReadOnlyChannel<T4> c4)
        {
            this.mux = new ReadOnlyChannelMux<T1, T2, T3>(c1, c2, c3);
            this.last = c4;
        }

        public void OnReceive(Func<(ChannelSelectResult<T1, T2, T3, T4> value, bool ok), bool> accept)
        {
            this.mux.OnReceive(t =>
            {
                return accept((new ChannelSelectResult<T1, T2, T3, T4>(t.value.Index, t.value.Item1, t.value.Item2, t.value.Item3, default), true));
            });

            this.last.OnReceive(t =>
            {
                return accept((new ChannelSelectResult<T1, T2, T3, T4>(3, default, default, default, ChannelSelectResult.Create(t)), true));
            });
        }
    }
}