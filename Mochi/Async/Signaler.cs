using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Mochi.Async
{
    public class Signaler<T>
    {
        private SignalAwaiter root = new SignalAwaiter();

        public void Signal(T value) => this.root.TryInvokeContinuation(value);

        public SignalAwaitable WaitSignaleAsync(CancellationToken cancellationToken)
            => new SignalAwaitable(new OneTimeSignalAwaiter(this.root, cancellationToken));

        public struct SignalAwaitable
        {
            private IAwaiter<T> awaiter;
            public SignalAwaitable(IAwaiter<T> awaiter) => this.awaiter = awaiter;

            public IAwaiter<T> GetAwaiter() => this.awaiter;
        }

        abstract class SignalAwaiterBase : IAwaiter<T>
        {
            public abstract bool IsCompleted { get; }
            private object continuation;
            protected object sync = new object();
            protected T result;

            public void TryInvokeContinuation(T result)
            {
                lock (this.sync)
                {
                    var _continuation = Interlocked.Exchange(ref this.continuation, null);
                    if (_continuation == null) return;

                    InvokeContinuation(_continuation, result);
                }
            }

            protected virtual void InvokeContinuation(object continuation, T result)
            {
                this.result = result;
                switch (continuation)
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
                    default:
                        break;
                }
            }

            public virtual T GetResult() => this.result;

            void INotifyCompletion.OnCompleted(Action continuation)
                => AddContinuation(continuation);

            void ICriticalNotifyCompletion.UnsafeOnCompleted(Action continuation)
                => AddContinuation(continuation);

            protected virtual void AddContinuation(Action continuation)
            {
                Action[] array = null;
                while (true)
                {
                    var _continuation = this.continuation;
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

                    if (Interlocked.CompareExchange(ref this.continuation, next, _continuation) == _continuation)
                    {
                        break;
                    }
                }
            }
        }

        class SignalAwaiter : SignalAwaiterBase
        {
            // signal awaiter is always not completed.
            public override bool IsCompleted => false;
            public override T GetResult() => this.result;
        }

        class OneTimeSignalAwaiter : SignalAwaiterBase
        {
            private bool isCompleted;
            public override bool IsCompleted => isCompleted;
            private CancellationToken cancellationToken;

            public OneTimeSignalAwaiter(IAwaiter<T> parent, CancellationToken cancellationToken)
            {
                this.cancellationToken = cancellationToken;
                if (this.cancellationToken.CanBeCanceled)
                {
                    var d = this.cancellationToken.Register(() =>
                    {
                        TryInvokeContinuation(default);
                    });

                    parent.UnsafeOnCompleted(() =>
                    {
                        TryInvokeContinuation(parent.GetResult());
                        d.Dispose();
                    });
                }
                else
                {
                    parent.UnsafeOnCompleted(() => TryInvokeContinuation(parent.GetResult()));
                }
            }

            public override T GetResult()
            {
                this.cancellationToken.ThrowIfCancellationRequested();
                return this.result;
            }

            protected override void InvokeContinuation(object continuation, T result)
            {
                if (this.isCompleted) return;

                this.isCompleted = true;
                base.InvokeContinuation(continuation, result);
            }

            protected override void AddContinuation(Action continuation)
            {
                if (this.isCompleted) continuation();
                else base.AddContinuation(continuation);
            }
        }
    }
}
