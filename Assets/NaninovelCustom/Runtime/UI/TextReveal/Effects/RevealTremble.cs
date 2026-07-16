using TMPro;
using UnityEngine;

namespace Naninovel.UI
{
    /// <summary>
    /// Jitters revealed characters with smooth pseudo-random noise each frame,
    /// optionally settling back to rest a set time after a character appears.
    /// Combine with RevealClipper for a trembling typewriter look.
    /// </summary>
    public class RevealTremble : TextRevealEffect
    {
        public float Amplitude { get => amplitude; set => amplitude = value; }
        public float Speed { get => speed; set => speed = value; }
        public float PerCharSeed { get => perCharSeed; set => perCharSeed = value; }
        public float EaseIn { get => easeIn; set => easeIn = value; }
        public float Settle { get => settle; set => settle = value; }

        [Tooltip("Maximum displacement in local units."), Range(0, 50)]
        [SerializeField] private float amplitude = 3f;
        [Tooltip("How fast the jitter changes."), Range(0, 30)]
        [SerializeField] private float speed = 12f;
        [Tooltip("Noise seed offset per character index, so neighbours tremble independently.")]
        [SerializeField] private float perCharSeed = 7.3f;
        [Tooltip("Seconds for a freshly revealed char to ramp into full amplitude."), Range(0, 2)]
        [SerializeField] private float easeIn = 0.1f;
        [Tooltip("Seconds after a char appears for the tremble to fade back to rest. 0 = tremble forever."), Range(0, 5)]
        [SerializeField] private float settle = 0f;

        private void OnDisable ()
        {
            if (Text) Text.ForceMeshUpdate();
        }

        private void LateUpdate ()
        {
            if (Text.textInfo.characterCount == 0) return;

            for (int i = 0; i <= Info.LastRevealedCharIndex && i < Text.textInfo.characterCount; i++)
                AnimateChar(Text.textInfo.characterInfo[i], i);

            Text.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);
        }

        private void AnimateChar (TMP_CharacterInfo info, int charIndex)
        {
            if (!info.isVisible) return;

            var elapsed = Engine.Time.Time - Info.GetAppendTime(charIndex);
            var ease = easeIn > 0 ? Mathf.Clamp01(elapsed / easeIn) : 1f;
            var fade = settle > 0 ? 1f - Mathf.Clamp01(elapsed / settle) : 1f;
            var weight = amplitude * ease * fade;
            if (weight <= 0f) return;

            var t = Engine.Time.Time * speed;
            var seed = charIndex * perCharSeed;
            // PerlinNoise returns [0,1]; remap to [-1,1] for a centred shake.
            var offset = new Vector3(
                (Mathf.PerlinNoise(seed, t) * 2f - 1f) * weight,
                (Mathf.PerlinNoise(t, seed) * 2f - 1f) * weight,
                0);

            var verts = Text.textInfo.meshInfo[info.materialReferenceIndex].vertices;
            verts[info.vertexIndex + 0] += offset;
            verts[info.vertexIndex + 1] += offset;
            verts[info.vertexIndex + 2] += offset;
            verts[info.vertexIndex + 3] += offset;
        }
    }
}
