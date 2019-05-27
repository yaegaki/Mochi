using System.Threading;

namespace Mochi.Async
{
    public class SyncFirst
    {
        private int v;

        public bool IsFinished
            => v != 0;

        public bool Try()
            => Interlocked.CompareExchange(ref v, 1, 0) == 0;
    }
}
