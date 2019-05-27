using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Mochi.Async
{
    public interface ISwitchCase
    {
        void TryInjectCancellation(CancellationToken cancellationToken);

        void ConditionUnsafeOnCompleted(Action action);
    }

    public interface ISwitchCaseMux<TSwitchCase, T>
        where TSwitchCase : ISwitchCase
    {
        SwitchCaseMux<SwitchCaseMux<TSwitchCase, T>, T> Mux(SwitchCase<T> other);
    }

    [AsyncMethodBuilder(typeof(SwitchCaseAsyncMethodBuilder<>))]
    public struct SwitchCase<T> : ISwitchCase
    {
        private Awaiter<T> awaiter;
        public ISwitchCaseCondition cond;

        public SwitchCase(ISwitchCaseCondition cond, Awaiter<T> awaiter)
            => (this.cond, this.awaiter) = (cond, awaiter);

        public Awaiter<T> GetAwaiter()
            => this.awaiter;
        
        public void TryInjectCancellation(CancellationToken cancellationToken)
            => this.cond?.TryInjectCancellation(cancellationToken);

        public SwitchCaseMux<SwitchCase<T>, T> Mux(SwitchCase<T> other)
            => new SwitchCaseMux<SwitchCase<T>, T>(this, other);

        public void ConditionUnsafeOnCompleted(Action action)
        {
            if (this.cond == null)
            {
                action();
            }
            else
            {
                this.cond.InnerAwaiterUnsafeOnCompleted(action);
            }
        }
    }

    public struct SwitchCaseTypeHint<T>
    {
    }

    public struct SwitchCaseMux<TSwitchCase, T> : ISwitchCase, ISwitchCaseMux<TSwitchCase, T>
        where TSwitchCase : ISwitchCase
    {
        public SwitchCaseTypeHint<TSwitchCase> SwitchCaseTypeHint => default;

        private TSwitchCase case1;
        private SwitchCase<T> case2;

        public SwitchCaseMux(TSwitchCase case1, SwitchCase<T> case2)
            => (this.case1, this.case2) = (case1, case2);

        public void TryInjectCancellation(CancellationToken cancellationToken)
        {
            this.case1.TryInjectCancellation(cancellationToken);
            this.case2.TryInjectCancellation(cancellationToken);
        }

        public void ConditionUnsafeOnCompleted(Action action)
        {
            var _action = action;
            void onCompleted()
            {
                if (_action == null) return;

                var __action = _action;
                if (Interlocked.CompareExchange(ref _action, null, _action) != __action)
                {
                    return;
                }

                __action();
            };

            this.case1.ConditionUnsafeOnCompleted(onCompleted);
            this.case2.ConditionUnsafeOnCompleted(onCompleted);
        }

        public SwitchCaseMux<SwitchCaseMux<TSwitchCase, T>, T> Mux(SwitchCase<T> other)
            => new SwitchCaseMux<SwitchCaseMux<TSwitchCase, T>, T>(this, other);
    }
}
