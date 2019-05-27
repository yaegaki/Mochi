using System;
using System.Collections.Generic;
using System.Threading;

namespace Mochi.Async
{
    public class Fiber : IAwaitable<Fiber>, IAwaiter
    {
        private object sync = new object();
        private Queue<Action> continuationQueue = new Queue<Action>();
        private int status = 0;

        public Fiber GetAwaiter()
            => this;
        
        public void GetResult()
        {
        }

        public bool IsCompleted
            => false;
        
        public void OnCompleted(Action continuation)
            =>AddContinuation(continuation);

        public void UnsafeOnCompleted(Action continuation)
            => AddContinuation(continuation);

        private void AddContinuation(Action continuation)
        {
            lock (this.sync)
            {
                if (this.status == 0)
                {
                    this.status = 1;
                }
                else
                {
                    this.continuationQueue.Enqueue(continuation);
                    return;
                }
            }

            while (true)
            {
                continuation();

                lock (this.sync)
                {
                    if (this.continuationQueue.Count > 0)
                    {
                        continuation = this.continuationQueue.Dequeue();
                    }
                    else
                    {
                        this.status = 0;
                        break;
                    }
                }
            }
        }
    }
}
