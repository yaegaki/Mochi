using System;
using System.Threading;

namespace Mochi.Async
{
    // value Task-like.(like System.ValueTask)
    public partial struct MochiTask : IAwaitable<Awaiter>
    {
        private IAwaiter objectAwaiter;
        private Awaiter valueAwaiter;

        public bool IsCompleted
            => this.objectAwaiter != null ? this.objectAwaiter.IsCompleted : this.valueAwaiter.IsCompleted;

        public MochiTask(IAwaiter awaiter)
        {
            this.objectAwaiter = awaiter;
            this.valueAwaiter = default;
        }

        public MochiTask(Awaiter awaiter)
        {
            this.objectAwaiter = null;
            this.valueAwaiter = awaiter;
        }

        public MochiTask(CancellationToken cancellationToken)
        {
            this.objectAwaiter = null;
            this.valueAwaiter = new Awaiter(cancellationToken);
        }

        public Awaiter GetAwaiter()
            => this.objectAwaiter != null ? new Awaiter(this.objectAwaiter) : this.valueAwaiter;

        public MochiTask ContinueWith(Action act)
        {
            var awaiter = Awaiter.Select<Awaiter, MochiUnit>(GetAwaiter(), () =>
            {
                act();
                return default;
            });

            return new MochiTask<MochiUnit>(awaiter);
        }
        
        public MochiTask<T> ContinueWith<T>(Func<T> func)
        {
            var awaiter = Awaiter.Select<Awaiter, T>(GetAwaiter(), () =>
            {
                return func();
            });

            return new MochiTask<T>(awaiter);
        }

        public static implicit operator MochiTask<MochiUnit>(MochiTask task)
            => task.ContinueWith(() => default(MochiUnit));
    }

    // value Task-like.(like System.ValueTask<T>)
    public struct MochiTask<T> : IAwaitable<Awaiter<T>>
    {
        private IAwaiter<T> objectAwaiter;
        private Awaiter<T> valueAwaiter;

        public bool IsCompleted
            => this.objectAwaiter != null ? this.objectAwaiter.IsCompleted : this.valueAwaiter.IsCompleted;

        public MochiTask(IAwaiter<T> awaiter)
        {
            this.objectAwaiter = awaiter;
            this.valueAwaiter = default;
        }

        public MochiTask(Awaiter<T> awaiter)
        {
            this.objectAwaiter = null;
            this.valueAwaiter = awaiter;
        }

        public MochiTask(T value)
        {
            this.objectAwaiter = null;
            this.valueAwaiter = new Awaiter<T>(value);
        }

        public MochiTask(CancellationToken cancellationToken)
        {
            this.objectAwaiter = null;
            this.valueAwaiter = new Awaiter<T>(cancellationToken);
        }

        public Awaiter<T> GetAwaiter()
            => this.objectAwaiter != null ? new Awaiter<T>(this.objectAwaiter) : this.valueAwaiter;

        public MochiTask<T> ContinueWith(Action<T> act)
        {
            var awaiter = Awaiter.Select<Awaiter<T>, T, T>(GetAwaiter(), t =>
            {
                act(t);
                return t;
            });

            return new MochiTask<T>(awaiter);
        }
        
        public MochiTask<K> ContinueWith<K>(Func<MochiTask<T>, K> func)
        {
            var awaiter = Awaiter.Select<Awaiter<T>, T, K>(GetAwaiter(), t =>
            {
                return func(new MochiTask<T>(t));
            });

            return new MochiTask<K>(awaiter);
        }

        public static implicit operator MochiTask(MochiTask<T> task)
            => new MochiTask(task.GetAwaiter());
    }
}
