using System;

namespace Mochi.Async
{
    public interface IAwaitable<TAwaiter>
    {
        TAwaiter GetAwaiter();
    }

    public struct Awaitable : IAwaitable<Awaiter>
    {
        private IAwaiter awaiter;

        public Awaitable(IAwaiter awaiter)
            => this.awaiter = awaiter;

        public Awaiter GetAwaiter()
            => this.awaiter != null ? new Awaiter(this.awaiter) : new Awaiter();
    }

    public struct Awaitable<T> : IAwaitable<Awaiter<T>>
    {
        private IAwaiter<T> awaiter;
        private T result;

        public Awaitable(IAwaiter<T> awaiter)
            => (this.awaiter, this.result) = (awaiter, default);

        public Awaitable(T value)
            => (this.awaiter, this.result) = (default, value);

        public Awaiter<T> GetAwaiter()
            => this.awaiter != null ? new Awaiter<T>(this.awaiter) : new Awaiter<T>(this.result);
        
        public Awaitable<K> Select<K>(Func<T, K> selector)
        {
            if (this.awaiter == null)
            {
                return new Awaitable<K>(selector(this.result));
            }
            else
            {
                var promise = new Promise<K>();
                var _awaiter = this.awaiter;
                _awaiter.UnsafeOnCompleted(() =>
                {
                    try
                    {
                        promise.TrySetResult(selector(_awaiter.GetResult()));
                    }
                    catch (OperationCanceledException e)
                    {
                        promise.TrySetCanceled(e.CancellationToken);
                    }
                    catch (Exception e)
                    {
                        promise.TrySetException(e);
                    }
                });

                return new Awaitable<K>(promise);
            }
        }
    }
}
