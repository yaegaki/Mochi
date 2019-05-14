using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Mochi.Async
{
    public class SequentialTaskRunner
    {
        private object sync = new object();
        private Queue<Func<Task>> taskQueue = new Queue<Func<Task>>();
        private TaskCompletionSource<int> promise = new TaskCompletionSource<int>();

        public void Enqueue(Func<Task> factory)
        {
            lock (this.sync)
            {
                this.taskQueue.Enqueue(factory);
                this.promise.TrySetResult(0);
            }
        }

        public async Task Run()
        {
            var _TaskQueue = new Queue<Func<Task>>();
            while (true)
            {
                await this.promise.Task;

                (this.taskQueue, _TaskQueue) = (_TaskQueue, this.taskQueue);
                this.promise = new TaskCompletionSource<int>();

                while (_TaskQueue.Count > 0)
                {
                    await _TaskQueue.Dequeue()();
                }
            }
        }
    }
}