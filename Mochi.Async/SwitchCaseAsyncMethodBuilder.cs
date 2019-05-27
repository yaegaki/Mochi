using System;
using System.Runtime.CompilerServices;

namespace Mochi.Async
{
    public struct SwitchCaseAsyncMethodBuilder<T>
    {
        private class StateMachineWrapper<TStateMachine>
            where TStateMachine : IAsyncStateMachine
        {
            public TStateMachine StateMachine;

            public void MoveNext() => this.StateMachine.MoveNext();
        }

        private Promise<T> promise;
        private T result;
        private Action continuation;
        private ISwitchCaseCondition cond;

        public static SwitchCaseAsyncMethodBuilder<T> Create()
            => new SwitchCaseAsyncMethodBuilder<T>();

        public SwitchCase<T> Task
        {
            get
            {
                if (this.promise == null)
                {
                    return new SwitchCase<T>(cond, new Awaiter<T>(this.result));;
                }

                return new SwitchCase<T>(cond, new Awaiter<T>(this.promise));
            }
        }

        public void SetException(Exception exception)
        {
            if (this.promise == null)
            {
                promise = new Promise<T>();
            }

            if (exception is OperationCanceledException e)
            {
                promise.TrySetCanceled(e.CancellationToken);
            }
            else
            {
                promise.TrySetException(exception);
            }
        }

        public void SetResult(T result)
        {
            if (this.promise == null)
            {
                this.result = result;
            }
            else
            {
                this.promise.TrySetResult(result);
            }
        }

        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : INotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            var firstAwait = this.promise == null;
            if (firstAwait)
            {
                this.promise = new Promise<T>();
                if (awaiter is ISwitchCaseCondition cond)
                {
                    this.cond = cond;
                }
                var w = new StateMachineWrapper<TStateMachine>();
                this.continuation = w.MoveNext;
                w.StateMachine = stateMachine;
            }

            awaiter.OnCompleted(this.continuation);
        }

        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : ICriticalNotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            var firstAwait = this.promise == null;
            if (firstAwait)
            {
                this.promise = new Promise<T>();
                if (awaiter is ISwitchCaseCondition cond)
                {
                    this.cond = cond;
                }
                var w = new StateMachineWrapper<TStateMachine>();
                this.continuation = w.MoveNext;
                w.StateMachine = stateMachine;
            }

            awaiter.UnsafeOnCompleted(this.continuation);
        }

        public void Start<TStateMachine>(ref TStateMachine stateMachine)
            where TStateMachine : IAsyncStateMachine
            => stateMachine.MoveNext();

        public void SetStateMachine(IAsyncStateMachine _)
        {
        }
    }
}
