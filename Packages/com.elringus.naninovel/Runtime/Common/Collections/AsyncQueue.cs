using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Naninovel
{
    public class AsyncQueue<T> : IReadOnlyCollection<T>, IDisposable
    {
        public int Count => queue.Count;

        private readonly Queue<T> queue = new();
        private readonly Semaphore semaphore = new(0);

        public void Enqueue (T item)
        {
            queue.Enqueue(item);
            semaphore.Release();
        }

        public async Awaitable<T> Wait (AsyncToken token)
        {
            while (!token.Canceled)
            {
                await semaphore.Wait(token.CancellationToken);
                if (queue.TryDequeue(out var message)) return message;
            }
            throw new AsyncOperationCanceledException(token);
        }

        public void Dispose () => semaphore.Dispose();
        public IEnumerator<T> GetEnumerator () => queue.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator () => GetEnumerator();
    }
}
