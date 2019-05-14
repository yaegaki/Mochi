using System.Collections.Generic;
using System.Threading.Tasks;

namespace Mochi.Async
{
    public class SequentialTaskRunner
    {
        private object sync = new object();
        private Queue<Task> taskQueue = new Queue<Task>();
        private TaskCompletionSource<int> promise = new TaskCompletionSource<int>();

        public void Enqueue(Task task)
        {
            lock (this.sync)
            {
                this.taskQueue.Enqueue(task);
                this.promise.TrySetResult(0);
            }
        }

        public Task Run()
        {
            return Task.Run(async () =>
            {
                var _TaskQueue = new Queue<Task>();
                while (true)
                {
                    await this.promise.Task;

                    (this.taskQueue, _TaskQueue) = (_TaskQueue, this.taskQueue);
                    this.promise = new TaskCompletionSource<int>();

                    while (_TaskQueue.Count > 0)
                    {
                        await _TaskQueue.Dequeue();
                    }
                }
            });
        }
    }
}