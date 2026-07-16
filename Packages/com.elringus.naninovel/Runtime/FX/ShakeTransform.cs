using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Naninovel.Commands;
using UnityEngine;

namespace Naninovel.FX
{
    /// <summary>
    /// Shakes a <see cref="Transform"/>.
    /// </summary>
    public abstract class ShakeTransform : MonoBehaviour, Spawn.IParameterized, Spawn.IAwaitable
    {
        public virtual string SpawnedPath { get; private set; }
        public virtual string ObjectName { get; private set; }
        public virtual int ShakesCount { get; private set; }
        public virtual bool Loop { get; private set; }
        public virtual float ShakeDuration { get; private set; }
        public virtual float DurationVariation { get; private set; }
        public virtual float ShakeAmplitude { get; private set; }
        public virtual float AmplitudeVariation { get; private set; }
        public virtual bool ShakeHorizontally { get; private set; }
        public virtual bool ShakeVertically { get; private set; }

        protected virtual int DefaultShakesCount => defaultShakesCount;
        protected virtual float DefaultShakeDuration => defaultShakeDuration;
        protected virtual bool DefaultLoop => defaultLoop;
        protected virtual float DefaultDurationVariation => defaultDurationVariation;
        protected virtual float DefaultShakeAmplitude => defaultShakeAmplitude;
        protected virtual float DefaultAmplitudeVariation => defaultAmplitudeVariation;
        protected virtual bool DefaultShakeHorizontally => defaultShakeHorizontally;
        protected virtual bool DefaultShakeVertically => defaultShakeVertically;

        protected virtual ISpawnManager SpawnManager => Engine.GetServiceOrErr<ISpawnManager>();
        protected virtual Vector3 DeltaPos { get; private set; }
        protected virtual Vector3 InitialPos { get; private set; }
        protected virtual Transform ShakenTransform { get; private set; }
        protected virtual Tweener<VectorTween> PositionTweener { get; } = new();
        protected virtual CancellationTokenSource CTS { get; private set; }

        [SerializeField] private int defaultShakesCount = 3;
        [SerializeField] private float defaultShakeDuration = .15f;
        [SerializeField] private bool defaultLoop;
        [SerializeField] private float defaultDurationVariation = .25f;
        [SerializeField] private float defaultShakeAmplitude = .5f;
        [SerializeField] private float defaultAmplitudeVariation = .5f;
        [SerializeField] private bool defaultShakeHorizontally;
        [SerializeField] private bool defaultShakeVertically = true;

        private Vector3 lastChangedPosition;

        public virtual void SetSpawnParameters (IReadOnlyList<string> parameters, bool asap)
        {
            if (PositionTweener.Running)
                PositionTweener.Complete();
            if (ShakenTransform)
                SetPositionSafe(InitialPos);

            SpawnedPath = gameObject.name;
            ObjectName = parameters?.ElementAtOrDefault(0);
            ShakesCount = parameters?.ElementAtOrDefault(1)?.AsInvariantInt() is { } p1 ? Mathf.Abs(p1) : DefaultShakesCount;
            Loop = parameters?.ElementAtOrDefault(2) is { } p2 ? bool.Parse(p2) : DefaultLoop;
            ShakeDuration = parameters?.ElementAtOrDefault(3)?.AsInvariantFloat() is { } p3 ? Mathf.Abs(p3) : DefaultShakeDuration;
            DurationVariation = parameters?.ElementAtOrDefault(4)?.AsInvariantFloat() is { } p4 ? Mathf.Clamp01(p4) : DefaultDurationVariation;
            ShakeAmplitude = parameters?.ElementAtOrDefault(5)?.AsInvariantFloat() is { } p5 ? Mathf.Abs(p5) : DefaultShakeAmplitude;
            AmplitudeVariation = parameters?.ElementAtOrDefault(6)?.AsInvariantFloat() is { } p6 ? Mathf.Clamp01(p6) : DefaultAmplitudeVariation;
            ShakeHorizontally = parameters?.ElementAtOrDefault(7) is { } p7 ? bool.Parse(p7) : DefaultShakeHorizontally;
            ShakeVertically = parameters?.ElementAtOrDefault(8) is { } p8 ? bool.Parse(p8) : DefaultShakeVertically;
        }

        public virtual async Awaitable AwaitSpawn (AsyncToken token = default)
        {
            ShakenTransform = GetShakenTransform();
            if (!ShakenTransform)
            {
                SpawnManager.DestroySpawned(SpawnedPath);
                Engine.Warn($"Failed to apply '{GetType().Name}' FX to '{ObjectName}': transform to shake not found.");
                return;
            }

            token = InitializeCTS(token);
            lastChangedPosition = InitialPos = ShakenTransform.position;
            DeltaPos = GetShakeDelta();

            if (Loop) LoopRoutine(token).Forget();
            else
            {
                for (int i = 0; i < ShakesCount; i++)
                    await ShakeSequence(token);
                if (SpawnManager.IsSpawned(SpawnedPath))
                    SpawnManager.DestroySpawned(SpawnedPath);
            }

            await Async.NextFrame(token); // Otherwise the subsequent shake won't work.
        }

        protected abstract Transform GetShakenTransform ();

        protected virtual Vector3 GetShakeDelta ()
        {
            return new(
                ShakeHorizontally ? ShakeAmplitude : 0,
                ShakeVertically ? ShakeAmplitude : 0);
        }

        protected virtual async Awaitable ShakeSequence (AsyncToken token)
        {
            var amplitude = DeltaPos + DeltaPos * Random.Range(-AmplitudeVariation, AmplitudeVariation);
            var duration = ShakeDuration + ShakeDuration * Random.Range(-DurationVariation, DurationVariation);
            await Move(InitialPos - amplitude * .5f, duration * .25f, token);
            await Move(InitialPos + amplitude, duration * .5f, token);
            await Move(InitialPos, duration * .25f, token);
        }

        protected virtual Awaitable Move (Vector3 position, float duration, AsyncToken token)
        {
            var tw = new VectorTween(ShakenTransform.position, position,
                new(duration, EasingType.SmoothStep), SetPositionSafe);
            return PositionTweener.Run(tw, token, ShakenTransform);
        }

        protected virtual void OnDestroy ()
        {
            Loop = false;
            CTS?.Cancel();
            CTS?.Dispose();

            SetPositionSafe(InitialPos);

            if (Engine.Initialized && SpawnManager.IsSpawned(SpawnedPath))
                SpawnManager.DestroySpawned(SpawnedPath);
        }

        protected virtual async Awaitable LoopRoutine (AsyncToken token)
        {
            // Ignore the completion token because it's complete when loading saved game and on rollback
            // with the intention to apply spawn parameters instantly, but in loop we don't care about that.
            token = new(token.CancellationToken);
            while (Loop && Application.isPlaying && token.EnsureNotCanceled())
                await ShakeSequence(token);
        }

        protected virtual AsyncToken InitializeCTS (AsyncToken token)
        {
            CTS?.Cancel();
            CTS?.Dispose();
            CTS = CancellationTokenSource.CreateLinkedTokenSource(token.CancellationToken);
            return new(CTS.Token, token.CompletionToken);
        }

        protected virtual void SetPositionSafe (Vector3 pos)
        {
            if (!ShakenTransform) return;

            // Only change the position in case the current value is the same as it was when we last changed it.
            // When different, it means something else has modified the position, and we should no longer touch it.
            // Happens when fast-forwarding through un-awaited @char and @shake commands.
            if (ShakenTransform.position != lastChangedPosition)
            {
                PositionTweener.Stop();
                return;
            }

            ShakenTransform.position = pos;
            // Assign 'ShakenTransform.position' to last (not 'pos'), as Unity may re-evaluate the set position;
            // for example, if we set 'pos' it'll displace the UI after shake if the UI has non-default scale factor.
            lastChangedPosition = ShakenTransform.position;
        }
    }
}
