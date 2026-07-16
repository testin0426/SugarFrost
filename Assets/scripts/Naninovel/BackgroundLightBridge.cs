using System.Collections.Generic;
using Naninovel;
using UnityEngine;

namespace QMLM.Naninovel
{
    /// <summary>
    /// Bridges a <see cref="GenericBackgroundBehaviour"/> appearance change to a
    /// directional light's intensity, animating the transition over <see cref="transitionDuration"/>.
    /// Drop on a generic background prefab next to <see cref="GenericBackgroundBehaviour"/>
    /// and assign the scene's directional light. Trigger from script with e.g. @back appearance:night.
    /// </summary>
    [RequireComponent(typeof(GenericBackgroundBehaviour))]
    public class BackgroundLightBridge : MonoBehaviour
    {
        [Tooltip("Directional light driven by background appearance changes.")]
        [SerializeField] private Light directionalLight;
        [Tooltip("Seconds to lerp from current intensity to the target.")]
        [SerializeField] private float transitionDuration = 1.5f;
        [Tooltip("Mapping from appearance keyword to target light intensity.")]
        [SerializeField] private List<Preset> presets = new List<Preset> {
            new Preset { appearance = "night", intensity = 0f },
            new Preset { appearance = "day", intensity = 1.5f },
            new Preset { appearance = "noon", intensity = 9f }
        };

        [System.Serializable]
        public struct Preset
        {
            public string appearance;
            public float intensity;
        }

        private GenericBackgroundBehaviour behaviour;
        private float fromIntensity;
        private float toIntensity;
        private float elapsed;
        private bool transitioning;

        private void Awake ()
        {
            behaviour = GetComponent<GenericBackgroundBehaviour>();
        }

        private void OnEnable ()
        {
            behaviour.OnAppearanceChanged += HandleAppearanceChanged;
        }

        private void OnDisable ()
        {
            behaviour.OnAppearanceChanged -= HandleAppearanceChanged;
        }

        private void Update ()
        {
            if (!transitioning || !directionalLight) return;
            elapsed += Time.deltaTime;
            var t = transitionDuration <= 0f ? 1f : Mathf.Clamp01(elapsed / transitionDuration);
            directionalLight.intensity = Mathf.Lerp(fromIntensity, toIntensity, t);
            if (t >= 1f) transitioning = false;
        }

        private void HandleAppearanceChanged (string appearance)
        {
            if (!directionalLight || string.IsNullOrEmpty(appearance)) return;
            if (!TryResolveIntensity(appearance, out var target)) return;
            fromIntensity = directionalLight.intensity;
            toIntensity = target;
            elapsed = 0f;
            transitioning = true;
        }

        private bool TryResolveIntensity (string appearance, out float intensity)
        {
            for (int i = 0; i < presets.Count; i++)
                if (string.Equals(presets[i].appearance, appearance, System.StringComparison.OrdinalIgnoreCase))
                {
                    intensity = presets[i].intensity;
                    return true;
                }
            if (float.TryParse(appearance, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out intensity))
                return true;
            intensity = 0f;
            return false;
        }
    }
}
