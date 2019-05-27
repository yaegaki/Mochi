using System;
using System.Threading;

namespace Mochi.Async
{
    public partial struct MochiTask
    {
        public static MochiTask<T> Switch<T>(
            Func<SwitchContext<MochiUnit>, SwitchCase<T>> case1Func,
            Func<SwitchContext<MochiUnit>, SwitchCase<T>> case2Func,
            CancellationTokenSource cancellationTokenSource = default)
            => Switch(default(MochiUnit), case1Func, case2Func, cancellationTokenSource);

        public static MochiTask<T> Switch<T>(
            Func<SwitchContext<MochiUnit>, SwitchCase<T>> case1Func,
            Func<SwitchContext<MochiUnit>, SwitchCase<T>> case2Func,
            Func<SwitchContext<MochiUnit>, SwitchCase<T>> case3Func,
            CancellationTokenSource cancellationTokenSource = default)
            => Switch(default(MochiUnit), case1Func, case2Func, case3Func, cancellationTokenSource);

        public static MochiTask<T> Switch<T>(
            Func<SwitchContext<MochiUnit>, SwitchCase<T>> case1Func,
            Func<SwitchContext<MochiUnit>, SwitchCase<T>> case2Func,
            Func<SwitchContext<MochiUnit>, SwitchCase<T>> case3Func,
            Func<SwitchContext<MochiUnit>, SwitchCase<T>> case4Func,
            CancellationTokenSource cancellationTokenSource = default)
            => Switch(default(MochiUnit), case1Func, case2Func, case3Func, case4Func, cancellationTokenSource);

        public static MochiTask<T> Switch<T>(
            Func<SwitchContext<MochiUnit>, SwitchCase<T>> case1Func,
            Func<SwitchContext<MochiUnit>, SwitchCase<T>> case2Func,
            Func<SwitchContext<MochiUnit>, SwitchCase<T>> case3Func,
            Func<SwitchContext<MochiUnit>, SwitchCase<T>> case4Func,
            Func<SwitchContext<MochiUnit>, SwitchCase<T>> case5Func,
            CancellationTokenSource cancellationTokenSource = default)
            => Switch(default(MochiUnit), case1Func, case2Func, case3Func, case4Func, case5Func, cancellationTokenSource);

        public static MochiTask<T> Switch<TCapture, T>(TCapture capture,
            Func<SwitchContext<TCapture>, SwitchCase<T>> case1Func,
            Func<SwitchContext<TCapture>, SwitchCase<T>> case2Func,
            CancellationTokenSource cancellationTokenSource = default)
        {
            var context = CreateContext(capture, cancellationTokenSource);

            var t1 = SwitchMux(context, case1Func, case2Func, cancellationTokenSource);

            return t1.task;
        }

        public static MochiTask<T> Switch<TCapture, T>(TCapture capture,
            Func<SwitchContext<TCapture>, SwitchCase<T>> case1Func,
            Func<SwitchContext<TCapture>, SwitchCase<T>> case2Func,
            Func<SwitchContext<TCapture>, SwitchCase<T>> case3Func,
            CancellationTokenSource cancellationTokenSource = default)
        {
            var context = CreateContext(capture, cancellationTokenSource);

            var t1 = SwitchMux(context, case1Func, case2Func, cancellationTokenSource);
            if (t1.task.IsCompleted) return t1.task;
            var t2 = SwitchMux(context, t1.syncFirst, t1.promise, t1.caseMux, t1.caseMux.SwitchCaseTypeHint, case3Func, cancellationTokenSource);

            return t2.task;
        }

        public static MochiTask<T> Switch<TCapture, T>(TCapture capture,
            Func<SwitchContext<TCapture>, SwitchCase<T>> case1Func,
            Func<SwitchContext<TCapture>, SwitchCase<T>> case2Func,
            Func<SwitchContext<TCapture>, SwitchCase<T>> case3Func,
            Func<SwitchContext<TCapture>, SwitchCase<T>> case4Func,
            CancellationTokenSource cancellationTokenSource = default)
        {
            var context = CreateContext(capture, cancellationTokenSource);

            var t1 = SwitchMux(context, case1Func, case2Func, cancellationTokenSource);
            if (t1.task.IsCompleted) return t1.task;
            var t2 = SwitchMux(context, t1.syncFirst, t1.promise, t1.caseMux, t1.caseMux.SwitchCaseTypeHint, case3Func, cancellationTokenSource);
            var t3 = SwitchMux(context, t2.syncFirst, t2.promise, t2.caseMux, t2.caseMux.SwitchCaseTypeHint, case4Func, cancellationTokenSource);

            return t3.task;
        }

        public static MochiTask<T> Switch<TCapture, T>(TCapture capture,
            Func<SwitchContext<TCapture>, SwitchCase<T>> case1Func,
            Func<SwitchContext<TCapture>, SwitchCase<T>> case2Func,
            Func<SwitchContext<TCapture>, SwitchCase<T>> case3Func,
            Func<SwitchContext<TCapture>, SwitchCase<T>> case4Func,
            Func<SwitchContext<TCapture>, SwitchCase<T>> case5Func,
            CancellationTokenSource cancellationTokenSource = default)
        {
            var context = CreateContext(capture, cancellationTokenSource);

            var t1 = SwitchMux(context, case1Func, case2Func, cancellationTokenSource);
            if (t1.task.IsCompleted) return t1.task;
            var t2 = SwitchMux(context, t1.syncFirst, t1.promise, t1.caseMux, t1.caseMux.SwitchCaseTypeHint, case3Func, cancellationTokenSource);
            var t3 = SwitchMux(context, t2.syncFirst, t2.promise, t2.caseMux, t2.caseMux.SwitchCaseTypeHint, case4Func, cancellationTokenSource);
            var t4 = SwitchMux(context, t3.syncFirst, t3.promise, t3.caseMux, t3.caseMux.SwitchCaseTypeHint, case5Func, cancellationTokenSource);

            return t4.task;
        }

        public static MochiTask<T> Switch<TCapture, T>(TCapture capture,
            Func<SwitchContext<TCapture>, SwitchCase<T>> case1Func,
            Func<SwitchContext<TCapture>, SwitchCase<T>> case2Func,
            Func<SwitchContext<TCapture>, SwitchCase<T>> case3Func,
            Func<SwitchContext<TCapture>, SwitchCase<T>> case4Func,
            Func<SwitchContext<TCapture>, SwitchCase<T>> case5Func,
            Func<SwitchContext<TCapture>, SwitchCase<T>> case6Func,
            CancellationTokenSource cancellationTokenSource = default)
        {
            var context = CreateContext(capture, cancellationTokenSource);

            var t1 = SwitchMux(context, case1Func, case2Func, cancellationTokenSource);
            if (t1.task.IsCompleted) return t1.task;
            var t2 = SwitchMux(context, t1.syncFirst, t1.promise, t1.caseMux, t1.caseMux.SwitchCaseTypeHint, case3Func, cancellationTokenSource);
            var t3 = SwitchMux(context, t2.syncFirst, t2.promise, t2.caseMux, t2.caseMux.SwitchCaseTypeHint, case4Func, cancellationTokenSource);
            var t4 = SwitchMux(context, t3.syncFirst, t3.promise, t3.caseMux, t3.caseMux.SwitchCaseTypeHint, case5Func, cancellationTokenSource);
            var t5 = SwitchMux(context, t4.syncFirst, t4.promise, t4.caseMux, t4.caseMux.SwitchCaseTypeHint, case6Func, cancellationTokenSource);

            return t5.task;
        }

        public static MochiTask<T> Switch<TCapture, T>(TCapture capture,
            Func<SwitchContext<TCapture>, SwitchCase<T>> case1Func,
            Func<SwitchContext<TCapture>, SwitchCase<T>> case2Func,
            Func<SwitchContext<TCapture>, SwitchCase<T>> case3Func,
            Func<SwitchContext<TCapture>, SwitchCase<T>> case4Func,
            Func<SwitchContext<TCapture>, SwitchCase<T>> case5Func,
            Func<SwitchContext<TCapture>, SwitchCase<T>> case6Func,
            Func<SwitchContext<TCapture>, SwitchCase<T>> case7Func,
            CancellationTokenSource cancellationTokenSource = default)
        {
            var context = CreateContext(capture, cancellationTokenSource);

            var t1 = SwitchMux(context, case1Func, case2Func, cancellationTokenSource);
            if (t1.task.IsCompleted) return t1.task;
            var t2 = SwitchMux(context, t1.syncFirst, t1.promise, t1.caseMux, t1.caseMux.SwitchCaseTypeHint, case3Func, cancellationTokenSource);
            var t3 = SwitchMux(context, t2.syncFirst, t2.promise, t2.caseMux, t2.caseMux.SwitchCaseTypeHint, case4Func, cancellationTokenSource);
            var t4 = SwitchMux(context, t3.syncFirst, t3.promise, t3.caseMux, t3.caseMux.SwitchCaseTypeHint, case5Func, cancellationTokenSource);
            var t5 = SwitchMux(context, t4.syncFirst, t4.promise, t4.caseMux, t4.caseMux.SwitchCaseTypeHint, case6Func, cancellationTokenSource);
            var t6 = SwitchMux(context, t5.syncFirst, t5.promise, t5.caseMux, t5.caseMux.SwitchCaseTypeHint, case7Func, cancellationTokenSource);

            return t6.task;
        }

        public static MochiTask<T> Switch<TCapture, T>(TCapture capture,
            Func<SwitchContext<TCapture>, SwitchCase<T>> case1Func,
            Func<SwitchContext<TCapture>, SwitchCase<T>> case2Func,
            Func<SwitchContext<TCapture>, SwitchCase<T>> case3Func,
            Func<SwitchContext<TCapture>, SwitchCase<T>> case4Func,
            Func<SwitchContext<TCapture>, SwitchCase<T>> case5Func,
            Func<SwitchContext<TCapture>, SwitchCase<T>> case6Func,
            Func<SwitchContext<TCapture>, SwitchCase<T>> case7Func,
            Func<SwitchContext<TCapture>, SwitchCase<T>> case8Func,
            CancellationTokenSource cancellationTokenSource = default)
        {
            var context = CreateContext(capture, cancellationTokenSource);

            var t1 = SwitchMux(context, case1Func, case2Func, cancellationTokenSource);
            if (t1.task.IsCompleted) return t1.task;
            var t2 = SwitchMux(context, t1.syncFirst, t1.promise, t1.caseMux, t1.caseMux.SwitchCaseTypeHint, case3Func, cancellationTokenSource);
            var t3 = SwitchMux(context, t2.syncFirst, t2.promise, t2.caseMux, t2.caseMux.SwitchCaseTypeHint, case4Func, cancellationTokenSource);
            var t4 = SwitchMux(context, t3.syncFirst, t3.promise, t3.caseMux, t3.caseMux.SwitchCaseTypeHint, case5Func, cancellationTokenSource);
            var t5 = SwitchMux(context, t4.syncFirst, t4.promise, t4.caseMux, t4.caseMux.SwitchCaseTypeHint, case6Func, cancellationTokenSource);
            var t6 = SwitchMux(context, t5.syncFirst, t5.promise, t5.caseMux, t5.caseMux.SwitchCaseTypeHint, case7Func, cancellationTokenSource);
            var t7 = SwitchMux(context, t6.syncFirst, t6.promise, t6.caseMux, t6.caseMux.SwitchCaseTypeHint, case8Func, cancellationTokenSource);

            return t6.task;
        }

        private static SwitchContext<TCapture> CreateContext<TCapture>(TCapture capture, CancellationTokenSource cancellationTokenSource)
        {
            var cancellationToken = cancellationTokenSource != null ? cancellationTokenSource.Token : CancellationToken.None;
            var context = new SwitchContext<TCapture>(capture, cancellationToken);
            return context;
        }

        private static (MochiTask<T> task, SyncFirst syncFirst, Promise<T> promise, SwitchCaseMux<SwitchCase<T>, T> caseMux) SwitchMux<TCapture, T>(SwitchContext<TCapture> context,
            Func<SwitchContext<TCapture>, SwitchCase<T>> case1Func,
            Func<SwitchContext<TCapture>, SwitchCase<T>> case2Func,
            CancellationTokenSource cancellationTokenSource)
            => SwitchMuxCore(context, case1Func, case2Func, cancellationTokenSource);


        private static (MochiTask<T> task, SyncFirst syncFirst, Promise<T> promise, SwitchCaseMux<SwitchCaseMux<TSwitchCase, T>, T> caseMux) SwitchMux<TCapture, TSwitchCaseMux, TSwitchCase, T>(SwitchContext<TCapture> context,
            SyncFirst syncFirst,
            Promise<T> promise,
            TSwitchCaseMux caseMux,
            SwitchCaseTypeHint<TSwitchCase> switchCaseTypeHint,
            Func<SwitchContext<TCapture>, SwitchCase<T>> otherCaseFunc,
            CancellationTokenSource cancellationTokenSource)
            where TSwitchCaseMux : ISwitchCase, ISwitchCaseMux<TSwitchCase, T>
            where TSwitchCase : ISwitchCase
        {
            if (syncFirst == null || syncFirst.IsFinished)
            {
                cancellationTokenSource?.Cancel();
                return (new MochiTask<T>(promise), syncFirst, promise, default);
            }

            var otherCase = otherCaseFunc(context);
            // otherCase is already completed
            if (otherCase.cond == null)
            {
                var canContinue = syncFirst.Try();
                cancellationTokenSource?.Cancel();
                caseMux.TryInjectCancellation(new CancellationToken(true));

                if (canContinue)
                {
                    promise.TrySetResult(otherCase.GetAwaiter());
                }

                return (new MochiTask<T>(promise), syncFirst, promise, default);
            }

            caseMux.ConditionUnsafeOnCompleted(() =>
            {
                otherCase.cond.TryInjectCancellation(new CancellationToken(true));
            });

            otherCase.cond.InnerAwaiterUnsafeOnCompleted(() =>
            {
                var canContinue = syncFirst.Try();
                cancellationTokenSource?.Cancel();

                if (canContinue)
                {
                    // cancel other case
                    caseMux.TryInjectCancellation(new CancellationToken(true));

                    // continue case
                    otherCase.cond.TryInvokeContinuation();
                    var awaiter = otherCase.GetAwaiter();
                    awaiter.UnsafeOnCompleted(() => promise.TrySetResult(awaiter));
                }
                else
                {
                    otherCase.cond.TryInjectCancellation(new CancellationToken(true));
                }
            });

            return (new MochiTask<T>(promise), syncFirst, promise, caseMux.Mux(otherCase));
        }

        private static (MochiTask<T> task, SyncFirst syncFirst, Promise<T> promise, SwitchCaseMux<SwitchCase<T>, T> caseMux) SwitchMuxCore<TCapture, T>(SwitchContext<TCapture> context,
            Func<SwitchContext<TCapture>, SwitchCase<T>> case1Func,
            Func<SwitchContext<TCapture>, SwitchCase<T>> case2Func,
            CancellationTokenSource cancellationTokenSource)
        {
            var case1 = case1Func(context);
            // case1 is already completed
            if (case1.cond == null)
            {
                cancellationTokenSource?.Cancel();

                return (new MochiTask<T>(case1.GetAwaiter()), default, default, default);
            }

            context.Index = 1;
            var case2 = case2Func(context);
            // case2 is already completed
            if (case2.cond == null)
            {
                cancellationTokenSource?.Cancel();
                case1.cond.TryInjectCancellation(new CancellationToken(true));

                return (new MochiTask<T>(case2.GetAwaiter()), default, default, default);
            }

            // await case1 and case2 condition
            var localCancelCount = 0;
            var syncFirst = new SyncFirst();
            var promise = new Promise<T>();
            case1.cond.InnerAwaiterUnsafeOnCompleted(() =>
            {
                var canContinue = syncFirst.Try();
                cancellationTokenSource?.Cancel();

                if (canContinue)
                {
                    // cancel other case
                    case2.cond.TryInjectCancellation(new CancellationToken(true));

                    // continue case
                    case1.cond.TryInvokeContinuation();
                    var awaiter = case1.GetAwaiter();
                    awaiter.UnsafeOnCompleted(() => promise.TrySetResult(awaiter));
                }
                else if (Interlocked.Increment(ref localCancelCount) == 2)
                {
                    case1.cond.TryInjectCancellation(new CancellationToken(true));
                    case2.cond.TryInjectCancellation(new CancellationToken(true));
                }
            });

            case2.cond.InnerAwaiterUnsafeOnCompleted(() =>
            {
                var canContinue = syncFirst.Try();
                cancellationTokenSource?.Cancel();

                if (canContinue)
                {
                    // cancel other case
                    case1.cond.TryInjectCancellation(new CancellationToken(true));

                    // continue case
                    case2.cond.TryInvokeContinuation();
                    var awaiter = case2.GetAwaiter();
                    awaiter.UnsafeOnCompleted(() => promise.TrySetResult(awaiter));
                }
                else if (Interlocked.Increment(ref localCancelCount) == 2)
                {
                    case1.cond.TryInjectCancellation(new CancellationToken(true));
                    case2.cond.TryInjectCancellation(new CancellationToken(true));
                }
            });

            return (new MochiTask<T>(promise), syncFirst, promise, case1.Mux(case2));
        }
    }
}
