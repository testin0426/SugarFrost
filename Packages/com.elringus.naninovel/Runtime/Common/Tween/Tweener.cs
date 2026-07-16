using System;
using JetBrains.Annotations;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// Performs tween animation of a <see cref="ITweenValue"/>.
    /// </summary>
    public interface ITweener<TTweenValue>
        where TTweenValue : struct, ITweenValue
    {
        TTweenValue Tween { get; }
        bool Running { get; }

        Awaitable Run (TTweenValue tween, AsyncToken token = default, UnityEngine.Object target = default);
        void Stop ();
        void Complete ();
    }

    /// <inheritdoc cref="ITweener{TTweenValue}"/>
    public class Tweener<TTweenValue> : ITweener<TTweenValue>
        where TTweenValue : struct, ITweenValue
    {
        public TTweenValue Tween { get; private set; }
        public bool Running { get; private set; }

        private float elapsedTime;
        private Guid lastRunGuid;
        [CanBeNull] private UnityEngine.Object target;

        public Awaitable Run (TTweenValue tween, AsyncToken token = default, UnityEngine.Object target = null)
        {
            if (Running) Complete();
            Tween = tween;
            this.target = target;
            if (tween.Props.Instant)
            {
                Complete();
                return Async.Completed;
            }
            return TweenAsync(token);
        }

        public void Stop ()
        {
            lastRunGuid = Guid.Empty;
            Running = false;
        }

        public void Complete ()
        {
            Stop();
            if (Tween.Props.Complete)
                Tween.Tween(1f);
        }

        protected async Awaitable TweenAsync (AsyncToken token = default)
        {
            PrepareTween();

            var currentRunGuid = lastRunGuid;
            while (elapsedTime <= Tween.Props.Duration &&
                   token.EnsureNotCanceledOrCompleted(target))
            {
                PerformTween();
                await Async.NextFrame(token);
                token.ThrowIfCanceled(target);
                if (lastRunGuid != currentRunGuid) return; // The tweener was completed instantly or stopped.
            }

            if (token.Completed) Complete();
            else FinishTween();
        }

        private void PrepareTween ()
        {
            Running = true;
            elapsedTime = 0f;
            lastRunGuid = Guid.NewGuid();
        }

        private void PerformTween ()
        {
            elapsedTime += Tween.Props.Scale ? Engine.Time.DeltaTime : Engine.Time.UnscaledDeltaTime;
            var tweenPercent = Mathf.Clamp01(elapsedTime / Tween.Props.Duration);
            Tween.Tween(tweenPercent);
        }

        private void FinishTween ()
        {
            Running = false;
        }
    }
}
