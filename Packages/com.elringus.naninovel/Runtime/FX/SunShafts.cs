using System.Collections.Generic;
using Naninovel.Commands;
using System.Linq;
using UnityEngine;

namespace Naninovel.FX
{
    [RequireComponent(typeof(ParticleSystem))]
    public class SunShafts : MonoBehaviour, Spawn.IParameterized, Spawn.IAwaitable, DestroySpawned.IParameterized, DestroySpawned.IAwaitable
    {
        protected float Intensity { get; private set; }
        protected float FadeInTime { get; private set; }
        protected float FadeOutTime { get; private set; }

        [SerializeField] private float defaultIntensity = .85f;
        [SerializeField] private float defaultFadeInTime = 3f;
        [SerializeField] private float defaultFadeOutTime = 3f;

        private static readonly int tintColorId = Shader.PropertyToID("_TintColor");

        private readonly Tweener<FloatTween> intensityTweener = new();
        private ParticleSystem particles;
        private Material particlesMaterial;
        private Color tintColor;

        public virtual void SetSpawnParameters (IReadOnlyList<string> parameters, bool asap)
        {
            Intensity = parameters?.ElementAtOrDefault(0)?.AsInvariantFloat() ?? defaultIntensity;
            FadeInTime = asap ? 0 : Mathf.Abs(parameters?.ElementAtOrDefault(1)?.AsInvariantFloat() ?? defaultFadeInTime);
        }

        public Awaitable AwaitSpawn (AsyncToken token = default)
        {
            if (intensityTweener.Running)
                intensityTweener.Complete();

            var time = token.Completed ? 0 : FadeInTime;
            var tw = new FloatTween(0, Intensity, new(time), SetIntensity);
            return intensityTweener.Run(tw, token, particles);
        }

        public void SetDestroyParameters (IReadOnlyList<string> parameters)
        {
            FadeOutTime = Mathf.Abs(parameters?.ElementAtOrDefault(0)?.AsInvariantFloat() ?? defaultFadeOutTime);
        }

        public Awaitable AwaitDestroy (AsyncToken token = default)
        {
            if (intensityTweener.Running)
                intensityTweener.Complete();

            var time = token.Completed ? 0 : FadeOutTime;
            var tw = new FloatTween(Intensity, 0, new(time), SetIntensity);
            return intensityTweener.Run(tw, token, particles);
        }

        private void Awake ()
        {
            particles = GetComponent<ParticleSystem>();
            particlesMaterial = GetComponent<ParticleSystemRenderer>().material;
            tintColor = particlesMaterial.GetColor(tintColorId);

            // Position before the first background.
            transform.position = new(0, 0, Engine.GetConfiguration<BackgroundsConfiguration>().ZOffset - 1);
            particles.Play(true); // Prevent prewarm particles from spawning before position change.
        }

        private void SetIntensity (float value)
        {
            var color = tintColor;
            color.a *= value;
            particlesMaterial.SetColor(tintColorId, color);
        }
    }
}
