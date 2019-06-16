using System.Collections.Generic;
using System.Threading;

namespace Mochi.Async
{
    public class Signaler
    {
        private object sync = new object();
        private List<Promise> promises = new List<Promise>();

        public void Signal()
        {
            lock (this.sync)
            {
                var currentLength = this.promises.Count;
                for (var i = 0; i < currentLength; i++)
                {
                    this.promises[i].TrySetResult();
                }

                // WaitSignalAsync was called while TrySetResult.
                if (this.promises.Count > currentLength)
                {
                    this.promises.RemoveRange(0, currentLength);
                }
                else
                {
                    this.promises.Clear();
                }
            }
        }

        public MochiTask WaitSignalAsync(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return new MochiTask(cancellationToken);
            }

            var promise = new Promise();
            if (cancellationToken.CanBeCanceled)
            {
                var d = cancellationToken.Register(() =>
                {
                    promise.TrySetCanceled(cancellationToken);
                });

                promise.OnCompleted(() => d.Dispose());
            }

            lock (this.sync)
            {
                this.promises.Add(promise);
            }

            return new MochiTask(promise);
        }
    }
}
