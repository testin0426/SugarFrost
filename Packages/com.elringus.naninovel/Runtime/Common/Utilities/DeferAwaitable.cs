using System;
using System.Threading.Tasks;
using UnityEngine;

namespace Naninovel
{
    /// <inheritdoc cref="Defer"/>
    public readonly struct DeferAwaitable : IAsyncDisposable
    {
        private readonly Func<Awaitable> fn;

        public DeferAwaitable (Func<Awaitable> fn)
        {
            this.fn = fn;
        }

        public static DeferAwaitable<TState> With<TState> (TState state, Func<TState, Awaitable> fn)
        {
            return new(state, fn);
        }

        public async ValueTask DisposeAsync () => await fn();
    }

    /// <inheritdoc cref="Defer"/>
    public readonly struct DeferAwaitable<TState> : IAsyncDisposable
    {
        private readonly TState state;
        private readonly Func<TState, Awaitable> fn;

        public DeferAwaitable (TState state, Func<TState, Awaitable> fn)
        {
            this.state = state;
            this.fn = fn;
        }

        public async ValueTask DisposeAsync () => await fn(state);
    }
}
