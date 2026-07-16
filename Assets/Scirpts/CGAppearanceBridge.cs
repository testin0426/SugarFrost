using UnityEngine;
using Naninovel;
using System.Collections.Generic;

/// <summary>
/// Variant of CharacterAppearanceBridge for a single CG object that hosts
/// multiple child Animators (e.g. two characters in one CG). Routes each
/// appearance trigger to every child Animator that declares it.
/// </summary>
[RequireComponent(typeof(GenericCharacterBehaviour))]
public class CGAppearanceBridge : MonoBehaviour
{
    private GenericCharacterBehaviour behaviour;
    private Animator[] animators;
    private readonly Dictionary<string, List<Animator>> triggerToAnimators = new();

    void Awake()
    {
        behaviour = GetComponent<GenericCharacterBehaviour>();

        animators = GetComponentsInChildren<Animator>();

        if (animators.Length == 0)
        {
            Debug.LogError("[CGBridge] No Animator found in children!");
            return;
        }

        foreach (var a in animators)
            foreach (var p in a.parameters)
                if (p.type == AnimatorControllerParameterType.Trigger)
                {
                    if (!triggerToAnimators.TryGetValue(p.name, out var list))
                        triggerToAnimators[p.name] = list = new List<Animator>();
                    list.Add(a);
                }

        Debug.Log($"[CGBridge] {animators.Length} animator(s), " +
                  $"Cached triggers: {string.Join(", ", triggerToAnimators.Keys)}");

        behaviour.OnAppearanceChanged += HandleAppearance;
    }

    void OnDestroy()
    {
        if (behaviour != null)
            behaviour.OnAppearanceChanged -= HandleAppearance;
    }

    private void HandleAppearance(string appearance)
    {
        var triggerName = appearance.Contains("/")
            ? appearance.Substring(appearance.IndexOf('/') + 1)
            : appearance;

        if (triggerToAnimators.TryGetValue(triggerName, out var list))
        {
            foreach (var a in list)
                a.SetTrigger(triggerName);
            Debug.Log($"[CGBridge] SetTrigger '{triggerName}' on {list.Count} animator(s) ✓");
        }
        else
            Debug.LogWarning($"[CGBridge] '{triggerName}' not in cache. " +
                             $"Cached: {string.Join(", ", triggerToAnimators.Keys)}");
    }
}
