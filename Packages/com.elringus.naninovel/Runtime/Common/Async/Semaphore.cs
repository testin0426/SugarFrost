using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// Replacement for <see cref="SemaphoreSlim"/> that runs on Unity scheduler.
    /// Required for platforms without threading support, such as WebGL.
    /// </summary>
    public class Semaphore : IDisposable
    {
        private readonly Queue<AwaitableCompletionSource> waiters = new();
        private readonly int maxCount;
        private int count;

        public Semaphore (int initialCount, int maxCount = int.MaxValue)
        {
            count = initialCount;
            this.maxCount = maxCount;
        }

        public async Awaitable<IDisposable> Wait (AsyncToken token = default)
        {
            token.ThrowIfCanceled();
            if (count > 0)
            {
                count--;
                return new Defer(Release);
            }

            var tcs = new AwaitableCompletionSource();
            await using var registration = token.CancellationToken.CanBeCanceled
                ? token.CancellationToken.Register(() => tcs.TrySetCanceled()) : default;
            waiters.Enqueue(tcs);
            await tcs.Awaitable;
            return new Defer(Release);
        }

        public void Release () => Release(1);

        public void Release (int releaseCount)
        {
            for (int i = 0; i < releaseCount; i++)
            {
                var released = false;
                while (waiters.TryDequeue(out var waiter)) // skip cancelled waiters
                    if (released = waiter.TrySetResult())
                        break;
                if (released) continue;
                if (count + 1 > maxCount) break;
                count++;
            }
        }

        public void Dispose ()
        {
            while (waiters.Count > 0)
                if (waiters.TryDequeue(out var waiter))
                    waiter.TrySetCanceled();
        }
    }
}
