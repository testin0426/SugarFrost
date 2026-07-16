using System.Collections;
using TMPro;
using UnityEngine;

namespace Naninovel
{
    public class PlaceholderCharacterBehaviour : LayeredCharacterBehaviour
    {
        [Header("Setup")]
        [SerializeField] private TextMeshPro idText;
        [SerializeField] private CircularText nameText;
        [SerializeField] private CircularText appearanceText;
        [SerializeField] private MeshRenderer circleRenderer;

        private static readonly int colorId = Shader.PropertyToID("_Color");
        private static readonly int outlineColorId = Shader.PropertyToID("_OutlineColor");
        private static readonly int outlineRotationId = Shader.PropertyToID("_OutlineGradientRotation");

        private Material mat;
        private Coroutine anim;

        public override void NotifyAppearanceChanged (string appearance) => SetAppearance(appearance);
        public override void NotifyLookDirectionChanged (CharacterLookDirection dir) => SetLookDirection(dir);

        public virtual void SetId (string id)
        {
            idText.SetText(id);
        }

        public virtual void SetName (string name)
        {
            nameText.SetText(name);
        }

        public virtual void SetAppearance (string appearance)
        {
            appearanceText.SetText(appearance);
        }

        public virtual void SetLookDirection (CharacterLookDirection dir)
        {
            if (dir == CharacterLookDirection.Center) return;
            if (anim != null) StopCoroutine(anim);
            var targetRotation = dir == CharacterLookDirection.Left ? 180f : 0f;
            anim = StartCoroutine(AnimateRotation(targetRotation));
        }

        public virtual void SetColor (Color color)
        {
            if (!mat) mat = circleRenderer.material;

            var circleColor = color / 8.5f;
            circleColor.a = .8f;
            mat.SetColor(colorId, circleColor);
            mat.SetColor(outlineColorId, color);

            idText.color = color;
            var desaturated = Color.Lerp(color, Color.white, .5f);
            desaturated.a = 1;
            nameText.SetColor(desaturated);
            appearanceText.SetColor(desaturated);
        }

        protected virtual IEnumerator AnimateRotation (float value)
        {
            const float duration = 0.3f;
            if (!mat) mat = circleRenderer.material;

            var startRotation = mat.GetFloat(outlineRotationId);
            var time = 0f;

            while (time < duration)
            {
                time += Time.deltaTime;
                var rotation = Mathf.Lerp(startRotation, value, time / duration);
                mat.SetFloat(outlineRotationId, rotation);
                yield return null;
            }

            mat.SetFloat(outlineRotationId, value);
            anim = null;
        }
    }
}
