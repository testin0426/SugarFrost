using System;
using System.Collections.Generic;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// Extend GenericBackgroundBehaviour to swap MeshRenderer materials
    /// when Naninovel routes an appearance change (e.g. @back 2D.classroom).
    /// Attach this to the root of your "2D" background prefab.
    /// </summary>
    public class Background2DBehaviour : GenericBackgroundBehaviour
    {
        [Serializable]
        public class AppearanceEntry
        {
            [Tooltip("Must match the appearance name used in @back, e.g. 'classroom'")]
            public string appearanceName;
            public Material material;
        }

        [Tooltip("The MeshRenderer on the plane whose material will be swapped.")]
        [SerializeField] private MeshRenderer planeRenderer;

        [Tooltip("Map each appearance name to a material.")]
        [SerializeField] private List<AppearanceEntry> appearances = new();

        // Fast lookup built at runtime
        private Dictionary<string, Material> _materialMap;

        private void Awake()
        {
            // Build dictionary for O(1) lookup
            _materialMap = new Dictionary<string, Material>(StringComparer.OrdinalIgnoreCase);
            foreach (var entry in appearances)
                if (!string.IsNullOrEmpty(entry.appearanceName) && entry.material != null)
                    _materialMap[entry.appearanceName] = entry.material;

            // Subscribe to the event that GenericActor fires
            OnAppearanceChanged += HandleAppearanceChanged;
        }

        private void OnDestroy()
        {
            OnAppearanceChanged -= HandleAppearanceChanged;
        }

        private void HandleAppearanceChanged(string appearanceName)
        {
            if (planeRenderer == null)
            {
                Debug.LogWarning("[Background2DBehaviour] No MeshRenderer assigned.", this);
                return;
            }

            if (_materialMap.TryGetValue(appearanceName, out var mat))
                planeRenderer.material = mat;
            else
                Debug.LogWarning($"[Background2DBehaviour] No material mapped for appearance '{appearanceName}'.", this);
        }

        // Editor helper — auto-find renderer on the same GameObject if not set
        private void Reset()
        {
            planeRenderer = GetComponent<MeshRenderer>();
        }
    }
}