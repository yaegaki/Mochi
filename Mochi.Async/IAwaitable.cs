using System;

namespace Mochi.Async
{
    public interface IAwaitable<TAwaiter>
    {
        TAwaiter GetAwaiter();
    }
}
