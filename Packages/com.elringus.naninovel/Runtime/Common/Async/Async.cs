using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// Helper utilities and extensions for common async operations and <see cref="Awaitable"/>.
    /// </summary>
    public static class Async
    {
        private static class ResultSource<T>
        {
            public static readonly AwaitableCompletionSource<T> Source = new();
        }

        private static readonly AwaitableCompletionSource cs = new();

        /// <summary>
        /// Gets an awaitable that has already completed successfully.
        /// </summary>
        /// <remarks>
        /// Remember that Unity's Awaitable objects are pooled under the hood, so it's not safe
        /// to cache and reuse (await) a single completed awaitable instance, hence we're creating
        /// a new one on each access here; it won't allocate on heap, because AwaitableCompletionSource
        /// updates internal pool on reset and returns a pooled instance that is safe to await again.
        /// </remarks>
        public static Awaitable Completed
        {
            get
            {
                cs.SetResult();
                var awaitable = cs.Awaitable;
                cs.Reset();
                return awaitable;
            }
        }

        /// <summary>
        /// Gets an awaitable that has already completed successfully with the specified result.
        /// </summary>
        public static Awaitable<T> Result<T> (T result)
        {
            var cs = ResultSource<T>.Source;
            cs.SetResult(result);
            var awaitable = cs.Awaitable;
            cs.Reset();
            return awaitable;
        }

        /// <summary>
        /// Ensures exceptions thrown during the awaitable execution are logged.
        /// </summary>
        public static async void Forget (this Awaitable awaitable)
        {
            try { await awaitable; }
            catch (OperationCanceledException) { }
            catch (Exception e) { Engine.Err($"Async exception: {e}"); }
        }

        /// <inheritdoc cref="Forget"/>
        public static async void Forget<T> (this Awaitable<T> awaitable)
        {
            try { await awaitable; }
            catch (OperationCanceledException) { }
            catch (Exception e) { Engine.Err($"Async exception: {e}"); }
        }

        /// <summary>
        /// Creates a continuation that executes when the target awaitable completes.
        /// </summary>
        public static async Awaitable Then (this Awaitable awaitable, Action action)
        {
            await awaitable;
            action();
        }

        /// <inheritdoc cref="Then"/>
        public static async Awaitable Then (this Awaitable awaitable, Func<Awaitable> fn)
        {
            await awaitable;
            await fn();
        }

        /// <inheritdoc cref="Then"/>
        public static async Awaitable Then<T> (this Awaitable<T> awaitable, Action<T> action)
        {
            var result = await awaitable;
            action(result);
        }

        /// <inheritdoc cref="Then"/>
        public static async Awaitable<R> Then<T, R> (this Awaitable<T> awaitable, Func<T, R> fn)
        {
            var result = await awaitable;
            return fn(result);
        }

        /// <inheritdoc cref="Then"/>
        public static async Awaitable Then<T> (this Awaitable<T> awaitable, Func<T, Awaitable> fn)
        {
            var result = await awaitable;
            await fn(result);
        }

        /// <summary>
        /// Switches execution to a background thread.
        /// </summary>
        public static async Awaitable ToBackground ()
        {
            await Awaitable.BackgroundThreadAsync();
        }

        /// <summary>
        /// Switches execution to the main Unity thread.
        /// </summary>
        public static async Awaitable ToMain ()
        {
            await Awaitable.MainThreadAsync();
        }

        /// <summary>
        /// Rents a pooled list for awaitables.
        /// Dispose of the returned object to return the list to the pool.
        /// </summary>
        public static IDisposable Rent (out List<Awaitable> tasks)
        {
            return ListPool<Awaitable>.Rent(out tasks);
        }

        /// <inheritdoc cref="Rent" />
        public static IDisposable Rent<T> (out List<Awaitable<T>> tasks)
        {
            return ListPool<Awaitable<T>>.Rent(out tasks);
        }

        /// <summary>
        /// Waits till the start of the next render loop iteration.
        /// </summary>
        public static Awaitable NextFrame (AsyncToken token = default)
        {
            // Don't use async/await here, as it causes heap allocation each frame.
            return Awaitable.NextFrameAsync(token.CancellationToken);
        }
        
        /// <summary>
        /// Waits till the end of the current render loop iteration.
        /// </summary>
        /// <remarks>
        /// Use instead of <see cref="NextFrame"/> to ensure the subsequent logic is applied after the
        /// current frame is rendered, which is useful to synchronize the rendering state at the cost of
        /// a potential for an additional input lag.
        /// </remarks>
        public static Awaitable EndOfFrame (AsyncToken token = default)
        {
            // Don't use async/await here, as it causes heap allocation each frame.
            return Awaitable.EndOfFrameAsync(token.CancellationToken);
        }

        /// <summary>
        /// Waits until the specified number of frames are rendered.
        /// </summary>
        public static async Awaitable Frames (int frameCount, AsyncToken token = default)
        {
            for (int i = 0; i < frameCount; i++)
            {
                await Awaitable.NextFrameAsync(token.CancellationToken);
                if (!token.EnsureNotCanceledOrCompleted()) return;
            }
        }

        /// <summary>
        /// Waits for the specified duration.
        /// </summary>
        public static async Awaitable Delay (TimeSpan time, AsyncToken token = default)
        {
            var end = Engine.Time.Time + time.TotalSeconds;
            while (Engine.Time.Time < end && token.EnsureNotCanceledOrCompleted())
                await Awaitable.NextFrameAsync(token.CancellationToken);
        }

        /// <summary>
        /// Waits for the specified duration ignoring current timescale.
        /// </summary>
        public static async Awaitable DelayUnscaled (TimeSpan time, AsyncToken token = default)
        {
            var end = Engine.Time.UnscaledTime + time.TotalSeconds;
            while (Engine.Time.UnscaledTime < end && token.EnsureNotCanceledOrCompleted())
                await Awaitable.NextFrameAsync(token.CancellationToken);
        }

        /// <summary>
        /// Waits while the specified condition is true.
        /// </summary>
        public static async Awaitable While (Func<bool> condition, AsyncToken token = default)
        {
            while (condition())
            {
                await Awaitable.NextFrameAsync(token.CancellationToken);
                if (!token.EnsureNotCanceledOrCompleted()) return;
            }
        }

        /// <summary>
        /// Waits until the specified condition is true.
        /// </summary>
        public static async Awaitable Until (Func<bool> condition, AsyncToken token = default)
        {
            while (!condition())
            {
                await Awaitable.NextFrameAsync(token.CancellationToken);
                if (!token.EnsureNotCanceledOrCompleted()) return;
            }
        }

        /// <summary>
        /// Returns an awaitable that completes when all the specified awaitables complete.
        /// </summary>
        public static async Awaitable All (IEnumerable<Awaitable> awaitables)
        {
            var list = awaitables.ToArray();
            var ex = default(Exception);
            for (int i = 0; i < list.Length; i++)
                try { await list[i]; }
                catch (Exception e) { ex ??= e; }
            if (ex != null) ExceptionDispatchInfo.Capture(ex).Throw();
        }

        /// <inheritdoc cref="All(IEnumerable{Awaitable})"/>
        public static Awaitable All (params Awaitable[] a) => All((IEnumerable<Awaitable>)a);

        /// <inheritdoc cref="All(IEnumerable{Awaitable})"/>
        public static async Awaitable<T[]> All<T> (IEnumerable<Awaitable<T>> awaitables)
        {
            var list = awaitables.ToArray();
            var results = new T[list.Length];
            var ex = default(Exception);
            for (int i = 0; i < list.Length; i++)
                try { results[i] = await list[i]; }
                catch (Exception e) { ex ??= e; }
            if (ex != null) ExceptionDispatchInfo.Capture(ex).Throw();
            return results;
        }

        /// <inheritdoc cref="All(IEnumerable{Awaitable})"/>
        public static Awaitable<T[]> All<T> (params Awaitable<T>[] a) => All((IEnumerable<Awaitable<T>>)a);

        /// <summary>
        /// Returns an awaitable that completes when any of the specified awaitables complete.
        /// </summary>
        public static Awaitable Any (IEnumerable<Awaitable> awaitables)
        {
            var list = awaitables.ToArray();
            var cs = new AwaitableCompletionSource();
            for (var i = 0; i < list.Length; i++)
                Run(cs, list[i]);
            return cs.Awaitable;

            static async void Run (AwaitableCompletionSource cs, Awaitable awaitable)
            {
                try { await awaitable; }
                catch (Exception e)
                {
                    cs.TrySetException(e);
                    return;
                }
                cs.TrySetResult();
            }
        }

        /// <inheritdoc cref="Any(IEnumerable{Awaitable})"/>
        public static Awaitable Any (params Awaitable[] a) => Any((IEnumerable<Awaitable>)a);

        /// <inheritdoc cref="Any(IEnumerable{Awaitable})"/>
        public static Awaitable<T> Any<T> (IEnumerable<Awaitable<T>> awaitables)
        {
            var list = awaitables.ToArray();
            var cs = new AwaitableCompletionSource<T>();
            for (var i = 0; i < list.Length; i++)
                Run(cs, list[i]);
            return cs.Awaitable;

            static async void Run (AwaitableCompletionSource<T> cs, Awaitable<T> awaitable)
            {
                try { cs.TrySetResult(await awaitable); }
                catch (Exception e) { cs.TrySetException(e); }
            }
        }

        /// <inheritdoc cref="Any(IEnumerable{Awaitable})"/>
        public static Awaitable<T> Any<T> (params Awaitable<T>[] a) => Any((IEnumerable<Awaitable<T>>)a);

        /// <summary>
        /// Allows awaiting <see cref="ResourceRequest"/> directly to get the loaded asset.
        /// </summary>
        public static Awaitable<UnityEngine.Object>.Awaiter GetAwaiter (this ResourceRequest request)
        {
            if (request.isDone) return Result(request.asset).GetAwaiter();
            var cs = new AwaitableCompletionSource<UnityEngine.Object>();
            request.completed += _ => cs.SetResult(request.asset);
            return cs.Awaitable.GetAwaiter();
        }

        /// <summary>
        /// Allows awaiting <see cref="IEnumerator"/> directly.
        /// </summary>
        public static Awaitable.Awaiter GetAwaiter (this IEnumerator enumerator)
        {
            return enumerator.ToAwaitable().GetAwaiter();
        }

        /// <summary>
        /// Converts the coroutine enumerator to an awaitable.
        /// </summary>
        public static Awaitable ToAwaitable (this IEnumerator coroutine, AsyncToken token = default)
        {
            var cs = new AwaitableCompletionSource();
            Engine.Behaviour.StartCoroutine(Runner(coroutine, cs, token));
            return cs.Awaitable;

            static IEnumerator Runner (IEnumerator coroutine, AwaitableCompletionSource cs, AsyncToken token)
            {
                var stack = new Stack<IEnumerator>();
                stack.Push(coroutine);

                while (stack.Count > 0)
                {
                    if (token.CancellationToken.IsCancellationRequested)
                    {
                        cs.SetCanceled();
                        yield break;
                    }

                    var currentEnumerator = stack.Peek();
                    var current = default(object);

                    try
                    {
                        if (!currentEnumerator.MoveNext())
                        {
                            stack.Pop();
                            continue;
                        }
                        current = currentEnumerator.Current;
                    }
                    catch (Exception e)
                    {
                        cs.SetException(e);
                        yield break;
                    }

                    if (current is IEnumerator nested) stack.Push(nested);
                    else yield return current;
                }

                cs.SetResult();
            }
        }

        /// <summary>
        /// Converts the awaitable to a coroutine enumerator.
        /// </summary>
        public static IEnumerator ToCoroutine (this Awaitable awaitable)
        {
            var awaiter = awaitable.GetAwaiter();
            while (!awaiter.IsCompleted) yield return null;
            try { awaiter.GetResult(); }
            catch (OperationCanceledException) { }
            catch (Exception e) { ExceptionDispatchInfo.Capture(e).Throw(); }
        }

        /// <summary>
        /// Invokes the function and converts the returned awaitable to a coroutine.
        /// </summary>
        public static IEnumerator ToCoroutine (Func<Awaitable> fn)
        {
            return fn().ToCoroutine();
        }

        /// <summary>
        /// Converts the awaitable to a standard .NET task.
        /// </summary>
        public static async Task ToTask (this Awaitable awaitable)
        {
            await awaitable;
        }

        /// <inheritdoc cref="ToTask"/>
        public static async Task<T> ToTask<T> (this Awaitable<T> awaitable)
        {
            return await awaitable;
        }
    }
}
