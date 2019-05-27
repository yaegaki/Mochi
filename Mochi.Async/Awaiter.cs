using System;
using System.Threading;

namespace Mochi.Async
{
    public struct Awaiter : IAwaiter
    {
        private IAwaiter awaiter;
        private (bool isCanceled, CancellationToken token) cancellation;
        private Exception exception;

        public bool IsCompleted => this.awaiter != null ? this.awaiter.IsCompleted : true;

        public Awaiter(IAwaiter awaiter)
            => (this.awaiter, this.cancellation, this.exception) = (awaiter, default, default);

        public Awaiter(CancellationToken cancellationToken)
            => (this.awaiter, this.cancellation, this.exception) = (default, (true, cancellationToken), default);

        public Awaiter(Exception exception)
            => (this.awaiter, this.cancellation, this.exception) = (default, default, exception);


        public void GetResult()
        {
            if (this.awaiter != null)
            {
                this.awaiter.GetResult();
                return;
            }

            if (this.cancellation.isCanceled)
            {
                throw new OperationCanceledException(this.cancellation.token);
            }
            else if (exception != null)
            {
                throw exception;
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

        public static ((bool isCanceled, CancellationToken token) cancellation, Exception exception) UnsafeGet<TAwaiter>(TAwaiter awaiter)
            where TAwaiter : IAwaiter
        {
            try
            {
                awaiter.GetResult();
                return default;
            }
            catch (OperationCanceledException e)
            {
                return ((true, e.CancellationToken), default);
            }
            catch (Exception e)
            {
                return (default, e);
            }
        }

        public static Awaiter<T> Select<TAwaiter, T>(TAwaiter awaiter, Func<T> selector)
            where TAwaiter : IAwaiter
        {
            if (awaiter.IsCompleted)
            {
                var d = UnsafeGet(awaiter);
                if (d.cancellation.isCanceled)
                {
                    return new Awaiter<T>(d.cancellation.token);
                }
                else if (d.exception != null)
                {
                    return new Awaiter<T>(d.exception);
                }

                return new Awaiter<T>(selector());
            }

            var promise = new Promise<T>();
            awaiter.UnsafeOnCompleted(() =>
            {
                var d = UnsafeGet(awaiter);
                if (d.cancellation.isCanceled)
                {
                    promise.TrySetCanceled(d.cancellation.token);
                }
                else if (d.exception != null)
                {
                    promise.TrySetException(d.exception);
                }
                else
                {
                    promise.TrySetResult(selector());
                }
            });
            return new Awaiter<T>(promise);
        }

        public static (T result, (bool isCanceled, CancellationToken token) cancellation, Exception exception) UnsafeGet<TAwaiter, T>(TAwaiter awaiter)
            where TAwaiter : IAwaiter<T>
        {
            try
            {
                var result = awaiter.GetResult();
                return (result, default, default);
            }
            catch (OperationCanceledException e)
            {
                return (default, (true, e.CancellationToken), default);
            }
            catch (Exception e)
            {
                return (default, default, e);
            }
        }

        public static Awaiter<K> Select<TAwaiter, T, K>(TAwaiter awaiter, Func<T, K> selector)
            where TAwaiter : IAwaiter<T>
        {
            if (awaiter.IsCompleted)
            {
                var d = UnsafeGet<TAwaiter, T>(awaiter);
                if (d.cancellation.isCanceled)
                {
                    return new Awaiter<K>(d.cancellation.token);
                }
                else if (d.exception != null)
                {
                    return new Awaiter<K>(d.exception);
                }

                return new Awaiter<K>(selector(d.result));
            }

            var promise = new Promise<K>();
            awaiter.UnsafeOnCompleted(() =>
            {
                var d = UnsafeGet<TAwaiter, T>(awaiter);
                if (d.cancellation.isCanceled)
                {
                    promise.TrySetCanceled(d.cancellation.token);
                }
                else if (d.exception != null)
                {
                    promise.TrySetException(d.exception);
                }
                else
                {
                    promise.TrySetResult(selector(d.result));
                }
            });
            return new Awaiter<K>(promise);
        }
    }

    public struct Awaiter<T> : IAwaiter<T>, IAwaiter
    {
        private IAwaiter<T> awaiter;
        private T result;
        private (bool isCanceled, CancellationToken token) cancellation;
        private Exception exception;


        public bool IsCompleted => this.awaiter != null ? awaiter.IsCompleted : true;

        public Awaiter(IAwaiter<T> awaiter)
            => (this.awaiter, this.result, this.cancellation, this.exception) = (awaiter, default, default, default);

        public Awaiter(T value)
            => (this.awaiter, this.result, this.cancellation, this.exception) = (default, value, default, default);

        public Awaiter(CancellationToken cancellationToken)
            => (this.awaiter, this.result, this.cancellation, this.exception) = (default, default, (true, cancellationToken), default);

        public Awaiter(Exception exception)
            => (this.awaiter, this.result, this.cancellation, this.exception) = (default, default, default, exception);

        void IAwaiter.GetResult()
            => GetResult();

        public T GetResult()
        {
            if (this.awaiter != null)
            {
                return this.awaiter.GetResult();
            }

            if (this.cancellation.isCanceled)
            {
                throw new OperationCanceledException(this.cancellation.token);
            }
            else if (exception != null)
            {
                throw exception;
            }

            return this.result;
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
}
