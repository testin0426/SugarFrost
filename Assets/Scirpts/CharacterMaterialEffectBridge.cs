using System;
using System.Collections.Generic;
using Naninovel;
using UnityEngine;
using UnityEngine.Rendering;

public interface IAppearanceMaterialEffect
{
    bool Matches(string appearance);
    void Apply(Renderer[] renderers);
    void Revert(Renderer[] renderers);
}

public enum MaterialNameMatch { Exact, Prefix, Suffix, Contains }

[Serializable]
public class MaterialZTestEffect : IAppearanceMaterialEffect
{
    [Tooltip("Appearance / animator trigger name that activates this effect (e.g. \"ache\").")]
    public string appearanceName = "ache";

    [Tooltip("Material name to target. Unity appends \" (Instance)\" at runtime, which is stripped before comparison.")]
    public string materialName = "eyeline";

    [Tooltip("How materialName is compared against the material's name.")]
    public MaterialNameMatch matchMode = MaterialNameMatch.Exact;

    [Tooltip("Shader property holding the ZTest compare function.")]
    public string zTestProperty = "_ZTestMode";

    [Tooltip("ZTest mode applied while this appearance is active.")]
    public CompareFunction activeZTest = CompareFunction.LessEqual;

    [Tooltip("ZTest mode restored when another appearance is selected.")]
    public CompareFunction defaultZTest = CompareFunction.Always;

    private readonly HashSet<Material> touched = new();

    public bool Matches(string appearance)
    {
        return !string.IsNullOrEmpty(appearance) &&
               !string.IsNullOrEmpty(appearanceName) &&
               appearance.Equals(appearanceName, StringComparison.OrdinalIgnoreCase);
    }

    public void Apply(Renderer[] renderers)
    {
        if (renderers == null || string.IsNullOrEmpty(materialName) || string.IsNullOrEmpty(zTestProperty))
            return;

        var propId = Shader.PropertyToID(zTestProperty);
        var value = (float)activeZTest;

        foreach (var r in renderers)
        {
            if (r == null) continue;
            var mats = r.materials;
            var changed = false;
            for (var i = 0; i < mats.Length; i++)
            {
                var m = mats[i];
                if (m == null) continue;
                if (!NameMatches(m.name)) continue;
                if (!m.HasProperty(propId)) continue;
                m.SetFloat(propId, value);
                touched.Add(m);
                changed = true;
            }
            if (changed) r.materials = mats;
        }
    }

    public void Revert(Renderer[] renderers)
    {
        if (touched.Count == 0 || string.IsNullOrEmpty(zTestProperty)) return;

        var propId = Shader.PropertyToID(zTestProperty);
        var value = (float)defaultZTest;

        foreach (var m in touched)
        {
            if (m == null) continue;
            if (!m.HasProperty(propId)) continue;
            m.SetFloat(propId, value);
        }
        touched.Clear();
    }

    private bool NameMatches(string runtimeName)
    {
        if (string.IsNullOrEmpty(runtimeName)) return false;

        // Strip the " (Instance)" suffix Unity adds to instanced materials.
        const string instanceSuffix = " (Instance)";
        if (runtimeName.EndsWith(instanceSuffix, StringComparison.Ordinal))
            runtimeName = runtimeName[..^instanceSuffix.Length];

        return matchMode switch
        {
            MaterialNameMatch.Exact => runtimeName.Equals(materialName, StringComparison.OrdinalIgnoreCase),
            MaterialNameMatch.Prefix => runtimeName.StartsWith(materialName, StringComparison.OrdinalIgnoreCase),
            MaterialNameMatch.Suffix => runtimeName.EndsWith(materialName, StringComparison.OrdinalIgnoreCase),
            MaterialNameMatch.Contains => runtimeName.IndexOf(materialName, StringComparison.OrdinalIgnoreCase) >= 0,
            _ => false,
        };
    }
}

[RequireComponent(typeof(GenericCharacterBehaviour))]
public class CharacterMaterialEffectBridge : MonoBehaviour
{
    [SerializeField]
    [Tooltip("Effects keyed off appearance/animator trigger names. Add more entries to drive other materials.")]
    private List<MaterialZTestEffect> effects = new() { new MaterialZTestEffect() };

    private GenericCharacterBehaviour behaviour;
    private Renderer[] renderers;
    private readonly List<IAppearanceMaterialEffect> runtimeEffects = new();

    void Awake()
    {
        behaviour = GetComponent<GenericCharacterBehaviour>();
        renderers = GetComponentsInChildren<Renderer>(true);

        runtimeEffects.Clear();
        foreach (var e in effects)
            if (e != null) runtimeEffects.Add(e);

        behaviour.OnAppearanceChanged += HandleAppearance;
    }

    void OnDestroy()
    {
        if (behaviour != null)
            behaviour.OnAppearanceChanged -= HandleAppearance;
    }

    public void RegisterEffect(IAppearanceMaterialEffect effect)
    {
        if (effect != null && !runtimeEffects.Contains(effect))
            runtimeEffects.Add(effect);
    }

    public void UnregisterEffect(IAppearanceMaterialEffect effect)
    {
        runtimeEffects.Remove(effect);
    }

    private void HandleAppearance(string appearance)
    {
        var triggerName = appearance != null && appearance.Contains("/")
            ? appearance.Substring(appearance.IndexOf('/') + 1)
            : appearance;

        foreach (var effect in runtimeEffects)
        {
            if (effect.Matches(triggerName)) effect.Apply(renderers);
            else effect.Revert(renderers);
        }
    }
}
