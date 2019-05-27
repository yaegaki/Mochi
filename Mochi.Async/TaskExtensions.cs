using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Mochi.Async
{
    public static class TaskExtensions
    {
        public static MochiTask ToMochiTask(this Task task)
        {
            var awaiter = task.GetAwaiter();
            if (awaiter.IsCompleted)
            {
                try
                {
                    awaiter.GetResult();
                    return new MochiTask();
                }
                catch (OperationCanceledException e)
                {
                    return new MochiTask(new Awaiter(e.CancellationToken));
                }
                catch (Exception e)
                {
                    return new MochiTask(new Awaiter(e));
                }
            }

            return new MochiTask(new TaskAwaiterWrapper(task.GetAwaiter()));
        }

        public static MochiTask<T> ToMochiTask<T>(this Task<T> task)
        {
            var awaiter = task.GetAwaiter();
            if (awaiter.IsCompleted)
            {
                try
                {
                    var result = awaiter.GetResult();
                    return new MochiTask<T>(result);
                }
                catch (OperationCanceledException e)
                {
                    return new MochiTask<T>(new Awaiter<T>(e.CancellationToken));
                }
                catch (Exception e)
                {
                    return new MochiTask<T>(new Awaiter<T>(e));
                }
            }

            return new MochiTask<T>(new TaskAwaiterWrapper<T>(task.GetAwaiter()));
        }
    }

    public class TaskAwaiterWrapper : IAwaiter
    {
        private TaskAwaiter awaiter;
        public bool IsCompleted => this.awaiter.IsCompleted;

        public TaskAwaiterWrapper(TaskAwaiter awaiter)
            => this.awaiter = awaiter;


        public void GetResult()
            => this.awaiter.GetResult();

        public void OnCompleted(Action continuation)
            => this.awaiter.OnCompleted(continuation);

        public void UnsafeOnCompleted(Action continuation)
            => this.awaiter.UnsafeOnCompleted(continuation);
    }

    public class TaskAwaiterWrapper<T> : IAwaiter<T>
    {
        private TaskAwaiter<T> awaiter;
        public bool IsCompleted => this.awaiter.IsCompleted;

        public TaskAwaiterWrapper(TaskAwaiter<T> awaiter)
            => this.awaiter = awaiter;


        public T GetResult()
            => this.awaiter.GetResult();

        public void OnCompleted(Action continuation)
            => this.awaiter.OnCompleted(continuation);

        public void UnsafeOnCompleted(Action continuation)
            => this.awaiter.UnsafeOnCompleted(continuation);
    }
}
