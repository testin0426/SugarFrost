using TMPro;
using UnityEngine;

namespace Naninovel.UI
{
    /// <summary>
    /// Displaces revealed characters along a sine wave each frame.
    /// Combine with RevealClipper for a wave + typewriter look.
    /// </summary>
    public class RevealWave : TextRevealEffect
    {
        public float Amplitude { get => amplitude; set => amplitude = value; }
        public float Frequency { get => frequency; set => frequency = value; }
        public float PhasePerChar { get => phasePerChar; set => phasePerChar = value; }
        public float EaseIn { get => easeIn; set => easeIn = value; }

        [Tooltip("Vertical amplitude in local units."), Range(0, 50)]
        [SerializeField] private float amplitude = 4f;
        [Tooltip("Wave cycles per second."), Range(0, 10)]
        [SerializeField] private float frequency = 2f;
        [Tooltip("Phase shift per character index (radians).")]
        [SerializeField] private float phasePerChar = 0.5f;
        [Tooltip("Seconds for a freshly revealed char to ramp into full amplitude."), Range(0, 2)]
        [SerializeField] private float easeIn = 0.25f;

        // Snapshot of the un-displaced (base) vertices, refreshed whenever TMP rebuilds the
        // mesh (eg, on each character reveal). We always displace from this snapshot rather than
        // accumulating into the live mesh, so a rebuild can't reset the wave.
        private TMP_MeshInfo[] baseMesh;

        private void OnEnable ()
        {
            TMPro_EventManager.TEXT_CHANGED_EVENT.Add(HandleTextChanged);
        }

        private void OnDisable ()
        {
            TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(HandleTextChanged);
            if (Text) Text.ForceMeshUpdate();
        }

        private void LateUpdate ()
        {
            if (Text.textInfo.characterCount == 0) return;
            if (baseMesh == null) CacheBaseMesh();

            for (int i = 0; i <= Info.LastRevealedCharIndex && i < Text.textInfo.characterCount; i++)
                AnimateChar(Text.textInfo.characterInfo[i], i);

            Text.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);
        }

        private void AnimateChar (TMP_CharacterInfo info, int charIndex)
        {
            if (!info.isVisible) return;

            var ease = easeIn > 0
                ? Mathf.Clamp01((Engine.Time.Time - Info.GetAppendTime(charIndex)) / easeIn)
                : 1f;
            var offset = new Vector3(
                0,
                Mathf.Sin(Engine.Time.Time * frequency * Mathf.PI * 2f + charIndex * phasePerChar) * amplitude * ease,
                0);

            var src = baseMesh[info.materialReferenceIndex].vertices;
            var dst = Text.textInfo.meshInfo[info.materialReferenceIndex].vertices;
            dst[info.vertexIndex + 0] = src[info.vertexIndex + 0] + offset;
            dst[info.vertexIndex + 1] = src[info.vertexIndex + 1] + offset;
            dst[info.vertexIndex + 2] = src[info.vertexIndex + 2] + offset;
            dst[info.vertexIndex + 3] = src[info.vertexIndex + 3] + offset;
        }

        private void HandleTextChanged (Object obj)
        {
            if (obj == Text) CacheBaseMesh();
        }

        private void CacheBaseMesh ()
        {
            baseMesh = Text.textInfo.CopyMeshInfoVertexData();
        }
    }
}
