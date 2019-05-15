using System;
using System.Runtime.CompilerServices;

namespace Mochi.Async
{
    public interface IAwaiter : ICriticalNotifyCompletion
    {
        bool IsCompleted { get; }
        void GetResult();
    }

    public interface IAwaiter<T> : ICriticalNotifyCompletion
    {
        bool IsCompleted { get; }
        T GetResult();
    }

    public struct Awaiter : IAwaiter
    {
        private IAwaiter awaiter;

        public bool IsCompleted => this.awaiter != null ? this.awaiter.IsCompleted : true;

        public Awaiter(IAwaiter awaiter)
            => this.awaiter = awaiter;

        public void GetResult()
        {
            if (this.awaiter != null)
            {
                this.awaiter.GetResult();
            }
        }
        public void OnCompleted(Action continuation)
        {
            if (this.IsCompleted)
            {
                continuation();
            }
            else
            {
                this.awaiter.OnCompleted(continuation);
            }
        }

        public void UnsafeOnCompleted(Action continuation)
        {
            if (this.IsCompleted)
            {
                continuation();
            }
            else
            {
                this.awaiter.UnsafeOnCompleted(continuation);
            }
        }
    }

    public struct Awaiter<T> : IAwaiter, IAwaiter<T>
    {
        private IAwaiter<T> awaiter;
        private T result;

        public bool IsCompleted => this.awaiter != null ? awaiter.IsCompleted : true;

        public Awaiter(IAwaiter<T> awaiter)
            => (this.awaiter, this.result) = (awaiter, default);

        public Awaiter(T value)
            => (this.awaiter, this.result) = (default, value);
        
        void IAwaiter.GetResult()
            => ((IAwaiter<T>)this).GetResult();

        public T GetResult()
            => this.awaiter != null ? this.awaiter.GetResult() : this.result;

        public void OnCompleted(Action continuation)
        {
            if (this.IsCompleted)
            {
                continuation();
            }
            else
            {
                this.awaiter.OnCompleted(continuation);
            }
        }

        public void UnsafeOnCompleted(Action continuation)
        {
            if (this.IsCompleted)
            {
                continuation();
            }
            else
            {
                this.awaiter.UnsafeOnCompleted(continuation);
            }
        }
    }
}
