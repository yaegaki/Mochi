using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Mochi.Async
{
    public interface ISwitchCaseCondition : ICriticalNotifyCompletion
    {
        bool TryInvokeContinuation();
        bool TryInjectCancellation(CancellationToken cancellationToken);

        void InnerAwaiterOnCompleted(Action action);
        void InnerAwaiterUnsafeOnCompleted(Action action);
    }

    public class SwitchCaseCondition<T> : ISwitchCaseCondition, IAwaiter<T>
    {
        private bool isUnsafe;
        private CancellationToken cancellationToken;
        private Action continuation;
        private Awaiter<T> innerAwaiter;
        // 0 == pending, 1 == Completed, 2 == InjectedCancel
        private int state;

        public bool IsCompleted => this.state > 0;
        public bool IsInjectedCancel => this.state == 2;

        public SwitchCaseCondition(bool isUnsafe, Awaiter<T> awaiter)
        {
            this.isUnsafe = isUnsafe;
            this.cancellationToken = default;
            this.continuation = default;
            this.innerAwaiter = awaiter;
            this.state = awaiter.IsCompleted ? 1 : 0;
        }
        
        public SwitchCaseCondition<T> GetAwaiter()
            => this;
        
        public T GetResult()
        {
            if (this.IsInjectedCancel)
            {
                throw new OperationCanceledException(this.cancellationToken);
            }

            return this.innerAwaiter.GetResult();
        }

        #region ISwitchCaseCondition

        public bool TryInvokeContinuation()
        {
            if (Interlocked.CompareExchange(ref this.state, 1, 0) != 0)
            {
                return false;
            }

            this.continuation();
            return true;
        }

        public bool TryInjectCancellation(CancellationToken cancellationToken)
        {
            if (Interlocked.CompareExchange(ref this.state, 2, 0) != 0)
            {
                return false;
            }

            // if Unsafe SwitchCase, don't call continuation
            if (this.isUnsafe) return true;

            this.continuation();
            return true;
        }

        public Awaiter<T> GetInnerAwaiter()
            => this.innerAwaiter;

        #endregion

        public void InnerAwaiterOnCompleted(Action action)
            => this.innerAwaiter.OnCompleted(action);

        public void InnerAwaiterUnsafeOnCompleted(Action action)
            => this.innerAwaiter.UnsafeOnCompleted(action);

        public void OnCompleted(Action continuation)
        {
            if (this.IsCompleted)
            {
                continuation();
                return;
            }
            else if (this.continuation != null)
            {
                throw new InvalidOperationException("Can not await twice.");
            }

            this.continuation = continuation;
        }

        public void UnsafeOnCompleted(Action continuation)
        {
            if (this.IsCompleted)
            {
                continuation();
                return;
            }
            else if (this.continuation != null)
            {
                throw new InvalidOperationException("Can not await twice.");
            }

            this.continuation = continuation;
        }
    }
}
