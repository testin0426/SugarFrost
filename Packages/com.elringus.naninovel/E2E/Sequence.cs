using System;
using System.Collections.Generic;
using UnityEngine;

namespace Naninovel.E2E
{
    /// <inheritdoc cref="ISequence"/>
    public class Sequence : ISequence
    {
        public object Current { get; private set; }

        private readonly List<Func<Awaitable>> initial = new();
        private readonly Queue<Func<Awaitable>> current = new();

        public bool MoveNext ()
        {
            if (current.Count == 0)
            {
                Reset();
                return false;
            }
            Current = current.Dequeue()().ToCoroutine();
            return true;
        }

        public void Reset ()
        {
            for (int i = initial.Count - 1; i >= 0; i--)
                current.Enqueue(initial[i]);
        }

        public ISequence Enqueue (Func<Awaitable> task)
        {
            initial.Add(task);
            current.Enqueue(task);
            return this;
        }

        public ISequence Enqueue (Action task) => Enqueue(() => {
            task();
            return Async.Completed;
        });
    }
}
