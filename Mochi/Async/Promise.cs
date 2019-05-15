using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Mochi.Async
{
    public class Promise<T> : IAwaitable<Promise<T>>, IAwaiter<T>, IAwaiter
    {
        private int status;
        private object continuation;
        private T result;
        private CancellationToken cancellationToken;
        private Exception exception;

        public bool IsSucceeded => status == 1 || status == 4;
        public bool IsCanceled => status == 2 || status == 5;
        public bool IsFaulted => status == 3 || status == 6;

        public bool TrySetResult(T result)
        {
            if (Interlocked.CompareExchange(ref this.status, 1, 0) != 0)
            {
                return false;
            }

            this.result = result;
            Volatile.Write(ref this.status, this.status + 3);
            InvokeContinuation();
            return true;
        }

        public bool TrySetCanceled(CancellationToken cancellationToken)
        {
            if (Interlocked.CompareExchange(ref this.status, 2, 0) != 0)
            {
                return false;
            }

            this.cancellationToken = cancellationToken;
            Volatile.Write(ref this.status, this.status + 3);
            InvokeContinuation();
            return true;
        }

        public bool TrySetException(Exception exception)
        {
            if (Interlocked.CompareExchange(ref this.status, 3, 0) != 0)
            {
                return false;
            }

            this.exception = exception;
            Volatile.Write(ref this.status, this.status + 3);
            InvokeContinuation();
            return true;
        }

        private void InvokeContinuation()
        {
            var _continuation = this.continuation;
            if (_continuation == null)
            {
                return;
            }

            if (Interlocked.CompareExchange(ref this.continuation, null, _continuation) != _continuation)
            {
                return;
            }

            switch (_continuation)
            {
                case Action action:
                    action();
                    break;
                case Action[] actions:
                    for (var i = 0; i < actions.Length; i++)
                    {
                        var action = actions[i];
                        if (action == null) break;
                        action();
                    }
                    break;
            }
        }

        #region IAwaitable<Promise<T>>

        Promise<T> IAwaitable<Promise<T>>.GetAwaiter() => this;

        #endregion

        #region IAwaiter

        void IAwaiter.GetResult() => GetResult();

        #endregion

        #region IAwaiter<T>

        public bool IsCompleted => this.status != 0;

        public T GetResult()
        {
            if (this.status < 3)
            {
                throw new InvalidOperationException("Not completed.");
            }

            while (true)
            {
                var _status = Volatile.Read(ref this.status) - 3;

                if (_status <= 0)
                {
                    System.Threading.Thread.Sleep(1);
                    continue;
                }

                switch (_status)
                {
                    case 1:
                        return this.result;
                    case 2:
                        throw new OperationCanceledException(this.cancellationToken);
                    case 3:
                        throw this.exception;
                    default:
                        throw new Exception("unknown bug.");
                }
            }
        }

        void INotifyCompletion.OnCompleted(Action continuation)
            => AddContinuation(continuation);

        void ICriticalNotifyCompletion.UnsafeOnCompleted(Action continuation)
            => AddContinuation(continuation);

        public void AddContinuation(Action continuation)
        {
            Action[] array = null;
            bool isSwaped = false;
            while (true)
            {
                var _status = Volatile.Read(ref this.status);
                if (_status > 0)
                {
                    while (true)
                    {
                        if (_status > 3)
                        {
                            if (isSwaped)
                            {
                                InvokeContinuation();
                            }
                            else
                            {
                                continuation();
                            }
                            return;
                        }

                        System.Threading.Thread.Sleep(1);
                        _status = Volatile.Read(ref this.status);
                    }
                }

                if (isSwaped) return;

                var _continuation = Volatile.Read(ref this.continuation);
                object next;
                switch (_continuation)
                {
                    case null:
                        next = continuation;
                        break;
                    case Action action:
                        if (array == null)
                        {
                            array = new Action[2];
                        }
                        else
                        {
                            for (var i = 2; i < array.Length; i++) array[i] = null;
                        }

                        array[0] = action;
                        array[1] = continuation;
                        next = array;
                        break;
                    case Action[] actions:
                        if (array == null || array.Length < actions.Length + 1)
                        {
                            array = new Action[actions.Length + 1];
                        }
                        else
                        {
                            for (var i = actions.Length + 1; i < array.Length; i++) array[i] = null;
                        }
                        Array.Copy(actions, array, actions.Length);
                        array[actions.Length] = continuation;
                        next = array;
                        break;
                    default:
                        return;
                }

                isSwaped = Interlocked.CompareExchange(ref this.continuation, next, _continuation) == _continuation;
            }
        }

        #endregion
    }
}
