using System.Threading.Tasks;

namespace Mochi.Async
{
    public static class SwitchContextExtensions
    {
        public static SwitchCaseCondition<MochiUnit> Case<TCapture>(this SwitchContext<TCapture> _, Task task)
            => Case(_, (MochiTask<MochiUnit>)task.ToMochiTask());

        public static SwitchCaseCondition<MochiUnit> UnsafeCase<TCapture>(this SwitchContext<TCapture> _, Task task)
            => UnsafeCase(_, (MochiTask<MochiUnit>)task.ToMochiTask());

        public static SwitchCaseCondition<T> Case<TCapture, T>(this SwitchContext<TCapture> _, Task<T> task)
            => Case(_, task.ToMochiTask());

        public static SwitchCaseCondition<T> UnsafeCase<TCapture, T>(this SwitchContext<TCapture> _, Task<T> task)
            => UnsafeCase(_, task.ToMochiTask());

        public static SwitchCaseCondition<T> Case<TCapture, T>(this SwitchContext<TCapture> _, MochiTask<T> task)
            => new SwitchCaseCondition<T>(false, task.GetAwaiter());

        public static SwitchCaseCondition<T> UnsafeCase<TCapture, T>(this SwitchContext<TCapture> _, MochiTask<T> task)
            => new SwitchCaseCondition<T>(true, task.GetAwaiter());
        
        public static SwitchCaseCondition<MochiUnit> Default<TCapture>(this SwitchContext<TCapture> _)
            => new SwitchCaseCondition<MochiUnit>(false, new Awaiter<MochiUnit>(default(MochiUnit)));

        public static SwitchCaseCondition<MochiUnit> UnsafeDefault<TCapture>(this SwitchContext<TCapture> _)
            => new SwitchCaseCondition<MochiUnit>(true, new Awaiter<MochiUnit>(default(MochiUnit)));
    }
}
