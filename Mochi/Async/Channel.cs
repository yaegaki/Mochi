using System;
using System.Collections.Generic;
using System.Threading;

namespace Mochi.Async
{
    public class Channel<T> : IChannel<T>, ILowLevelReadOnlyChannel<T>, IDisposable
    {
        private object sync = new object();
        private bool isDisposed;
        private int capacity;
        private Queue<T> queue;
        private Queue<Promise<int>> enqueuePromiseQueue = new Queue<Promise<int>>();
        private Queue<(Func<(T value, bool), bool> accept, Promise<(T value, bool ok)> promise)> dequeuePromiseQueue = new Queue<(Func<(T value, bool ok), bool> accept, Promise<(T value, bool ok)> promise)>();

        public Channel() : this(0)
        {
        }

        public Channel(int capacity)
        {
            if (capacity < 0) throw new ArgumentException("capacity must be greater than -1");
            if (capacity == 0) capacity = 1; 

            this.queue = new Queue<T>();
            this.capacity = capacity;
        }

        public int Count()
        {
            return this.queue.Count;
        }

        public Awaitable SendAsync(T value, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                var p = new Promise<int>();
                p.TrySetCanceled(cancellationToken);
                return new Awaitable((IAwaiter)p);
            }

            lock (this.sync)
            {
                if (this.isDisposed)
                {
                    var p = new Promise<int>();
                    p.TrySetException(new InvalidOperationException("closed channel."));
                    return new Awaitable((IAwaiter)p);
                }

                if (this.queue.Count == 0)
                {
                    while (this.dequeuePromiseQueue.Count > 0)
                    {
                        var (accept, dequeuePromise) = this.dequeuePromiseQueue.Dequeue();
                        if (accept != null)
                        {
                            if (accept((value, true)))
                            {
                                return new Awaitable();
                            }
                        }
                        else
                        {
                            if (dequeuePromise.TrySetResult((value, true)))
                            {
                                return new Awaitable();
                            }
                        }
                    }
                }


                // can complete immediate.
                if (this.queue.Count < this.capacity)
                {
                    this.queue.Enqueue(value);
                    return new Awaitable();
                }

                // need await dequeue.
                var promise = new Promise<int>();
                this.enqueuePromiseQueue.Enqueue(promise);
                if (cancellationToken.CanBeCanceled)
                {
                    var d = cancellationToken.Register(() => promise.TrySetCanceled(cancellationToken));
                    promise.AddContinuation(() =>
                    {
                        if (promise.IsSucceeded)
                        {
                            this.queue.Enqueue(value);
                        }
                        d.Dispose();
                    });
                }
                else
                {
                    promise.AddContinuation(() => this.queue.Enqueue(value));
                }

                return new Awaitable(promise);
            }
        }

        public void OnReceive(Func<(T value, bool ok), bool> accept)
        {
            lock (this.sync)
            {
                if (this.isDisposed)
                {
                    accept(default);
                    return;
                }

                if (this.queue.Count > 0)
                {
                    var v = (this.queue.Peek(), true);
                    if (!accept(v)) return;

                    this.queue.Dequeue();
                    while (this.enqueuePromiseQueue.Count > 0)
                    {
                        var enqueuePromise = this.enqueuePromiseQueue.Dequeue();
                        if (enqueuePromise.TrySetResult(0))
                        {
                            break;
                        }
                    }

                    return;
                }

                this.dequeuePromiseQueue.Enqueue((accept, null));
            }
        }

        public Awaitable<(T value, bool ok)> ReceiveAsync(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                var p = new Promise<(T, bool)>();
                p.TrySetCanceled(cancellationToken);
                return new Awaitable<(T, bool)>(p);
            }

            lock (this.sync)
            {
                if (this.isDisposed)
                {
                    return new Awaitable<(T, bool)>();
                }

                if (this.queue.Count > 0)
                {
                    var result = new Awaitable<(T, bool)>((this.queue.Dequeue(), true));
                    while (this.enqueuePromiseQueue.Count > 0)
                    {
                        var enqueuePromise = this.enqueuePromiseQueue.Dequeue();
                        if (enqueuePromise.TrySetResult(0))
                        {
                            break;
                        }
                    }

                    return result;
                }

                var promise = new Promise<(T, bool)>();
                this.dequeuePromiseQueue.Enqueue((null, promise));
                if (cancellationToken.CanBeCanceled)
                {
                    var d = cancellationToken.Register(() => promise.TrySetCanceled(cancellationToken));
                    promise.AddContinuation(() => d.Dispose());
                }

                return new Awaitable<(T, bool)>(promise);
            }
        }

        public void Dispose()
        {
            lock (this.sync)
            {
                if (this.isDisposed)
                {
                    return;
                }

                this.isDisposed = true;
                while (this.enqueuePromiseQueue.Count > 0)
                {
                    this.enqueuePromiseQueue.Dequeue().TrySetException(new InvalidOperationException("closed channel."));
                }

                while (this.dequeuePromiseQueue.Count > 0)
                {
                    var (accept, promise) = this.dequeuePromiseQueue.Dequeue();
                    if (accept != null)
                    {
                        accept(default);
                    }
                    else
                    {
                        promise.TrySetResult(default);
                    }
                }
            }
        }
    }
}
