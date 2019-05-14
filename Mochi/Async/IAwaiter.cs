using System.Runtime.CompilerServices;

namespace Mochi.Async
{
    public interface IAwaiter<T> : ICriticalNotifyCompletion
    {
        bool IsCompleted { get; }
        T GetResult();
    }
}
