using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Naninovel.UI
{
    /// <summary>
    /// Text reveal effect that pops characters in one-by-one with no fade gradient:
    /// revealed chars are fully opaque, unrevealed chars are fully transparent.
    /// <para>
    /// Self-paced: the reveal velocity is driven by <see cref="charsPerSecond"/> on this
    /// component rather than by Naninovel's reveal-speed settings. The effect never reveals
    /// ahead of what the engine permits, and snaps to full when the line is finished, skipped,
    /// or completed by user input.
    /// </para>
    /// Use as a drop-in alternative to <see cref="RevealFader"/> (remove the fader from the printer).
    /// </summary>
    public class RevealPop : TextRevealEffect
    {
        [Tooltip("How many characters to pop in per second. Higher is faster.")]
        [SerializeField, Range(1f, 120f)] private float charsPerSecond = 20f;
        [Tooltip("Whether to respect <alpha> tags or otherwise custom opacity levels assigned to specific text characters. When enabled, the effect uses the per-character authored alpha as the visible value instead of 255. Adds work inside the reveal loop.")]
        [SerializeField] private bool supportCustomOpacity;

        private readonly Dictionary<int, byte> customOpacity = new();
        // Number of characters currently popped in (fractional; floored when rendering). -1 = none.
        private float revealed = -1f;

        private void OnEnable ()
        {
            TMPro_EventManager.TEXT_CHANGED_EVENT.Add(HandleTextChanged);
        }

        private void OnDisable ()
        {
            TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(HandleTextChanged);
        }

        private void LateUpdate ()
        {
            if (Text.textInfo.characterCount == 0) return;
            Advance();
            Render();
        }

        private void Advance ()
        {
            var last = Text.textInfo.characterCount - 1;
            var cap = Info.LastRevealedCharIndex;
            if (cap < revealed) revealed = cap;                                       // new line / reset / rollback
            else if (cap >= last) revealed = cap;                                     // finished / skipped / completed
            else revealed = Mathf.Min(cap, revealed + charsPerSecond * Engine.Time.DeltaTime);
        }

        private void Render ()
        {
            var shownUpTo = Mathf.FloorToInt(revealed);
            for (int i = 0; i < Text.textInfo.characterCount; i++)
                SetCharOpacity(Text.textInfo.characterInfo[i], i <= shownUpTo ? GetMax(i) : (byte)0);
            Text.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
        }

        private void SetCharOpacity (TMP_CharacterInfo info, byte opacity)
        {
            if (!info.isVisible) return;
            var colors = Text.textInfo.meshInfo[info.materialReferenceIndex].colors32;
            for (int i = 0; i < 4; i++)
                colors[info.vertexIndex + i].a = opacity;
        }

        private byte GetMax (int charIndex)
        {
            return supportCustomOpacity && customOpacity.TryGetValue(charIndex, out var custom) ? custom : byte.MaxValue;
        }

        private void HandleTextChanged (Object obj)
        {
            if (obj != Text) return;
            if (supportCustomOpacity) ResolveCustomOpacity();
        }

        private void ResolveCustomOpacity ()
        {
            customOpacity.Clear();
            for (int i = 0; i < Text.textInfo.characterCount; i++)
            {
                var charInfo = Text.textInfo.characterInfo[i];
                var meshInfo = Text.textInfo.meshInfo[charInfo.materialReferenceIndex];
                var color = meshInfo.colors32[charInfo.vertexIndex];
                customOpacity[i] = color.a;
            }
        }
    }
}
